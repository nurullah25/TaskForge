import { suggestKey } from './project-create-dialog';

describe('suggestKey', () => {
  it('uses initials for multi-word names', () => {
    expect(suggestKey('Customer Portal')).toBe('CP');
    expect(suggestKey('mobile app v2')).toBe('MAV');
  });

  it('uses the first three letters for single words', () => {
    expect(suggestKey('Website')).toBe('WEB');
  });

  it('never starts with a digit and ignores punctuation', () => {
    expect(suggestKey('2024 Roadmap')).toBe('R');
    expect(suggestKey('  ')).toBe('');
  });
});
