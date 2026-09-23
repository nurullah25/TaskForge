import { Component, computed, input } from '@angular/core';
import { ChartSlice } from '../donut-chart/donut-chart';

@Component({
  selector: 'app-bar-chart',
  template: `
    <ul class="bars">
      @for (bar of bars(); track bar.name) {
        <li>
          <span class="name">{{ bar.name }}</span>
          <span class="track">
            <span
              class="fill"
              [style.width.%]="bar.percentage"
              [style.background]="bar.color"
            ></span>
          </span>
          <strong class="count">{{ bar.count }}</strong>
        </li>
      } @empty {
        <li class="muted">Nothing to show yet</li>
      }
    </ul>
  `,
  styles: `
    .bars {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 7px 0;
      font-size: 14px;
    }

    .name {
      width: 74px;
      flex-shrink: 0;
    }

    .track {
      flex: 1;
      height: 10px;
      border-radius: 999px;
      background: var(--tf-border);
      overflow: hidden;
    }

    .fill {
      display: block;
      height: 100%;
      border-radius: 999px;
      transition: width 0.25s ease;
    }

    .count {
      width: 28px;
      text-align: right;
    }
  `,
})
export class BarChart {
  readonly slices = input.required<ChartSlice[]>();

  // Bars are drawn relative to the biggest value, so small counts stay visible.
  protected readonly bars = computed(() => {
    const max = Math.max(1, ...this.slices().map((slice) => slice.count));
    return this.slices().map((slice) => ({ ...slice, percentage: (slice.count / max) * 100 }));
  });
}
