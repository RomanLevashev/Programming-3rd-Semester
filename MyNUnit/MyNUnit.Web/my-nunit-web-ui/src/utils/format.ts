export function formatDuration(ms: number) {
  if (!isFinite(ms)) return '-';
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)} с`;
  return `${ms.toFixed(1)} мс`;
}

export function formatDate(value: string) {
  const date = new Date(value);
  return date.toLocaleString();
}
