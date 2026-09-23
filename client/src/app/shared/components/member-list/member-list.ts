import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserAvatar } from '../user-avatar/user-avatar';

export interface MemberRow {
  userId: number;
  fullName: string;
  email: string;
  role: string;
}

// Used for both organization and project members. The parent decides which roles
// exist and who may be changed; the API still validates every change.
@Component({
  selector: 'app-member-list',
  imports: [MatSelectModule, MatButtonModule, MatIconModule, MatTooltipModule, UserAvatar],
  templateUrl: './member-list.html',
  styleUrl: './member-list.scss',
})
export class MemberList {
  readonly members = input.required<MemberRow[]>();
  readonly roles = input.required<string[]>();
  readonly currentUserId = input<number>();
  readonly canEdit = input<(member: MemberRow) => boolean>(() => false);

  readonly roleChange = output<{ member: MemberRow; role: string }>();
  readonly remove = output<MemberRow>();
}
