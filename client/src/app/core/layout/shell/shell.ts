import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

// Layout for every signed-in page: sidebar on the left, top bar and page content on the right.
// On small screens the sidebar turns into a slide-over menu.
@Component({
  selector: 'app-shell',
  imports: [MatSidenavModule, RouterOutlet, Sidebar, Topbar],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly sidenav = viewChild.required(MatSidenav);

  protected readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall, Breakpoints.Small])
      .pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  protected toggleMenu(): void {
    this.sidenav().toggle();
  }

  protected closeMenuOnHandset(): void {
    if (this.isHandset()) {
      this.sidenav().close();
    }
  }
}
