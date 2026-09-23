import { RelativeTimePipe } from './relative-time.pipe';

describe('RelativeTimePipe', () => {
  const pipe = new RelativeTimePipe();

  function minutesAgo(minutes: number): string {
    return new Date(Date.now() - minutes * 60_000).toISOString();
  }

  it('is empty for a missing date', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('describes recent times in words', () => {
    expect(pipe.transform(minutesAgo(0.5))).toBe('just now');
    expect(pipe.transform(minutesAgo(1))).toBe('1 minute ago');
    expect(pipe.transform(minutesAgo(40))).toBe('40 minutes ago');
    expect(pipe.transform(minutesAgo(60))).toBe('1 hour ago');
    expect(pipe.transform(minutesAgo(60 * 30))).toBe('1 day ago');
  });

  it('falls back to a date once a week has passed', () => {
    const old = new Date('2020-03-04T10:00:00Z');

    expect(pipe.transform(old.toISOString())).toContain('2020');
  });
});
