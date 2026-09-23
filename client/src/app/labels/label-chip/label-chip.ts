import { Component, input } from '@angular/core';
import { Label } from '../label.models';

@Component({
  selector: 'app-label-chip',
  template: ` <span class="label" [style.--label-color]="label().color">{{ label().name }}</span> `,
  styles: `
    .label {
      display: inline-block;
      padding: 1px 8px;
      border: 1px solid color-mix(in srgb, var(--label-color) 40%, transparent);
      border-radius: 6px;
      background: color-mix(in srgb, var(--label-color) 14%, transparent);
      color: color-mix(in srgb, var(--label-color) 75%, black);
      font-size: 11px;
      font-weight: 600;
      line-height: 17px;
      white-space: nowrap;
    }
  `,
})
export class LabelChip {
  readonly label = input.required<Label>();
}
