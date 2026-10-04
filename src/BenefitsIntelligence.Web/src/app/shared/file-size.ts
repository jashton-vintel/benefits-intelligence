const UNITS = ['bytes', 'KB', 'MB', 'GB'];

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} ${bytes === 1 ? 'byte' : 'bytes'}`;
  }

  let size = bytes;
  let unit = 0;
  while (size >= 1024 && unit < UNITS.length - 1) {
    size /= 1024;
    unit++;
  }

  // One decimal place for small values ("1.5 MB"), none once it adds nothing ("240 KB").
  const digits = size < 10 ? 1 : 0;
  return `${size.toFixed(digits)} ${UNITS[unit]}`;
}
