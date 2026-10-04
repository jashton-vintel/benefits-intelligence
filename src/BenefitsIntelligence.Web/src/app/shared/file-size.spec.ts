import { formatFileSize } from './file-size';

describe('formatFileSize', () => {
  it.each([
    [0, '0 bytes'],
    [1, '1 byte'],
    [900, '900 bytes'],
    [1024, '1.0 KB'],
    [18_432, '18 KB'],
    [1_572_864, '1.5 MB'],
    [20 * 1024 * 1024, '20 MB'],
  ])('formats %d bytes as %s', (bytes, expected) => {
    expect(formatFileSize(bytes)).toBe(expected);
  });
});
