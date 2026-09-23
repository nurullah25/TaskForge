import { Component, inject, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { UserAvatar } from '../../../shared/components/user-avatar/user-avatar';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-topbar',
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    UserAvatar,
  ],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
})
export class Topbar {
  private readonly auth = inject(AuthService);

  readonly showMenuButton = input(false);
  readonly menuClicked = output();

  protected readonly user = this.auth.currentUser;

  protected logout(): void {
    this.auth.logout();
  }
}
