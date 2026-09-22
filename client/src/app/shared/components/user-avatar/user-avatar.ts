import { Component, computed, input } from '@angular/core';

const COLORS = ['#2563eb', '#7c3aed', '#db2777', '#ea580c', '#059669', '#0891b2', '#4f46e5', '#b45309'];

@Component({
  selector: 'app-user-avatar',
  template: `
    <span
      class="avatar"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [style.font-size.px]="size() * 0.4"
      [style.background]="color()"
      [attr.title]="name()"
      [attr.aria-label]="name()"
    >
      {{ initials() }}
    </span>
  `,
  styles: `
    .avatar {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      color: #fff;
      font-weight: 600;
      user-select: none;
      flex-shrink: 0;
    }
  `,
})
export class UserAvatar {
  readonly name = input.required<string>();
  readonly size = input(32);

  protected readonly initials = computed(() =>
    this.name()
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0].toUpperCase())
      .join(''),
  );

  // Same name always gets the same color, so people are easy to recognise on a board.
  protected readonly color = computed(() => {
    const hash = [...this.name()].reduce((sum, char) => sum + char.charCodeAt(0), 0);
    return COLORS[hash % COLORS.length];
  });
}
