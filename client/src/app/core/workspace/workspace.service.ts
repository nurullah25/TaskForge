import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, combineLatest, of, switchMap, tap } from 'rxjs';
import { canManageOrganization, Organization } from '../../organizations/organization.models';
import { OrganizationService } from '../../organizations/organization.service';
import { Project } from '../../projects/project.models';
import { ProjectService } from '../../projects/project.service';
import { AuthService } from '../auth/auth.service';

const SELECTED_ORGANIZATION_KEY = 'taskforge.organizationId';

// App-wide state for "where am I working": the user's organizations, the selected one,
// and its projects. The sidebar, dashboard and project pages all read from here.
@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private readonly auth = inject(AuthService);
  private readonly organizationApi = inject(OrganizationService);
  private readonly projectApi = inject(ProjectService);

  readonly organizations = signal<Organization[]>([]);
  readonly organizationsLoaded = signal(false);
  private readonly selectedId = signal<number | null>(readSelectedId());

  // Typed explicitly: indexing an empty array gives undefined, which TypeScript
  // would otherwise hide behind the Organization type.
  readonly currentOrganization = computed<Organization | null>(() => {
    const organizations = this.organizations();
    return organizations.find((o) => o.id === this.selectedId()) ?? organizations[0] ?? null;
  });

  readonly canManageCurrentOrganization = computed(() =>
    canManageOrganization(this.currentOrganization()?.myRole),
  );

  private readonly currentOrganizationId = computed(() => this.currentOrganization()?.id ?? null);
  private readonly reloadProjects$ = new BehaviorSubject<void>(undefined);
  readonly projectsLoading = signal(false);

  // Reloads whenever the selected organization changes or reloadProjects() is called.
  // switchMap drops the response of an older request if the user switches quickly.
  readonly projects = toSignal(
    combineLatest([toObservable(this.currentOrganizationId), this.reloadProjects$]).pipe(
      tap(() => this.projectsLoading.set(true)),
      switchMap(([organizationId]) =>
        organizationId === null
          ? of([])
          : this.projectApi.list(organizationId).pipe(catchError(() => of([]))),
      ),
      tap(() => this.projectsLoading.set(false)),
    ),
    { initialValue: [] as Project[] },
  );

  private readonly userId = computed(() => this.auth.currentUser()?.id ?? null);

  constructor() {
    effect(() => {
      const userId = this.userId();
      untracked(() => (userId === null ? this.clear() : this.loadOrganizations()));
    });
  }

  loadOrganizations(): void {
    this.organizationApi.list().subscribe((organizations) => {
      this.organizations.set(organizations);
      this.organizationsLoaded.set(true);
    });
  }

  selectOrganization(id: number): void {
    this.selectedId.set(id);
    try {
      localStorage.setItem(SELECTED_ORGANIZATION_KEY, String(id));
    } catch {
      // Storage can be unavailable (private mode). Remembering the choice is only a convenience.
    }
  }

  saveOrganization(organization: Organization): void {
    this.organizations.update((list) => {
      const others = list.filter((o) => o.id !== organization.id);
      return [...others, organization].sort((a, b) => a.name.localeCompare(b.name));
    });
    this.selectOrganization(organization.id);
  }

  removeOrganization(id: number): void {
    this.organizations.update((list) => list.filter((o) => o.id !== id));
  }

  reloadProjects(): void {
    this.reloadProjects$.next();
  }

  private clear(): void {
    this.organizations.set([]);
    this.organizationsLoaded.set(false);
  }
}

function readSelectedId(): number | null {
  try {
    const value = Number(localStorage.getItem(SELECTED_ORGANIZATION_KEY));
    return value > 0 ? value : null;
  } catch {
    return null;
  }
}
