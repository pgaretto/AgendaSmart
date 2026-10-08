const pad = (n) => String(n).padStart(2, '0')

// Fecha local en formato yyyy-MM-dd (sin pasar por UTC).
export const toDateKey = (date) =>
  `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`

// Fecha-hora local sin zona horaria, como la espera el backend (DateTime).
export const toLocalIso = (date) => `${toDateKey(date)}T00:00:00`

// Grilla del mes: semanas de lunes a domingo que cubren todo el mes.
export function getMonthGrid(year, month) {
  const first = new Date(year, month, 1)
  const offset = (first.getDay() + 6) % 7
  const start = new Date(year, month, 1 - offset)
  const daysInMonth = new Date(year, month + 1, 0).getDate()
  const weeks = Math.ceil((offset + daysInMonth) / 7)

  return Array.from({ length: weeks }, (_, w) =>
    Array.from({ length: 7 }, (_, d) => new Date(start.getFullYear(), start.getMonth(), start.getDate() + w * 7 + d)),
  )
}
