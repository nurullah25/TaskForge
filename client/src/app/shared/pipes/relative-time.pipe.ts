import { Pipe, PipeTransform } from '@angular/core';

const MINUTE = 60_000;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = typeof value === 'string' ? new Date(value) : value;
    const elapsed = Date.now() - date.getTime();

    if (elapsed < MINUTE) {
      return 'just now';
    }
    if (elapsed < HOUR) {
      return plural(Math.floor(elapsed / MINUTE), 'minute');
    }
    if (elapsed < DAY) {
      return plural(Math.floor(elapsed / HOUR), 'hour');
    }
    if (elapsed < 7 * DAY) {
      return plural(Math.floor(elapsed / DAY), 'day');
    }

    return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }
}

function plural(count: number, unit: string): string {
  return `${count} ${unit}${count === 1 ? '' : 's'} ago`;
}
