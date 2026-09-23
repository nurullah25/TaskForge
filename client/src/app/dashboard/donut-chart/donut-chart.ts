import { Component, computed, input } from '@angular/core';

export interface ChartSlice {
  name: string;
  count: number;
  color: string;
}

// A small SVG donut. A chart library would be a lot of weight for two charts,
// and stroke-dasharray does the job in a few lines.
@Component({
  selector: 'app-donut-chart',
  template: `
    @let total = totalCount();
    <div class="chart">
      <svg viewBox="0 0 42 42" role="img" [attr.aria-label]="label()">
        <circle class="track" cx="21" cy="21" r="15.9" />
        @for (arc of arcs(); track arc.name) {
          <circle
            cx="21"
            cy="21"
            r="15.9"
            [attr.stroke]="arc.color"
            [attr.stroke-dasharray]="arc.length + ' ' + (100 - arc.length)"
            [attr.stroke-dashoffset]="arc.offset"
          />
        }
        <text x="21" y="20.5" class="value">{{ total }}</text>
        <text x="21" y="25" class="caption">{{ centerLabel() }}</text>
      </svg>

      <ul class="legend">
        @for (slice of slices(); track slice.name) {
          <li>
            <span class="dot" [style.background]="slice.color"></span>
            {{ slice.name }}
            <strong>{{ slice.count }}</strong>
          </li>
        } @empty {
          <li class="muted">Nothing to show yet</li>
        }
      </ul>
    </div>
  `,
  styleUrl: './donut-chart.scss',
})
export class DonutChart {
  readonly slices = input.required<ChartSlice[]>();
  readonly label = input('Chart');
  readonly centerLabel = input('total');

  protected readonly totalCount = computed(() =>
    this.slices().reduce((sum, slice) => sum + slice.count, 0),
  );

  // Each arc is a circle drawn with a dash the length of its share of the circumference.
  protected readonly arcs = computed(() => {
    const total = this.totalCount();
    if (total === 0) {
      return [];
    }

    let used = 0;
    return this.slices()
      .filter((slice) => slice.count > 0)
      .map((slice) => {
        const length = (slice.count / total) * 100;
        const arc = { name: slice.name, color: slice.color, length, offset: 25 - used };
        used += length;
        return arc;
      });
  });
}
