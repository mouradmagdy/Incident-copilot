export function formatNumber(value: number, maximumFractionDigits = 0) {
  return new Intl.NumberFormat('en-US', { maximumFractionDigits }).format(value)
}

export function formatTime(value: string | number) {
  return new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function getLogLevel(level: number | string) {
  if (typeof level === 'string') return level

  return ['Debug', 'Information', 'Warning', 'Error', 'Critical'][level] ?? 'Unknown'
}
