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

// Semana (lunes a domingo) que contiene a la fecha dada.
export function getWeek(date) {
  const offset = (date.getDay() + 6) % 7
  return Array.from(
    { length: 7 },
    (_, d) => new Date(date.getFullYear(), date.getMonth(), date.getDate() - offset + d),
  )
}

export const addDays = (date, n) => new Date(date.getFullYear(), date.getMonth(), date.getDate() + n)

export const parseDateKey = (key) => {
  const [y, m, d] = key.split('-').map(Number)
  return new Date(y, m - 1, d)
}

// Valor para <input type="datetime-local"> (yyyy-MM-ddTHH:mm) a partir de un string del backend.
export const toDateTimeInput = (iso) => iso.slice(0, 16)

// Valor para <input type="date"> (yyyy-MM-dd) a partir de un string del backend.
export const toDateInput = (iso) => iso.slice(0, 10)

// De los inputs del navegador al DateTime sin zona que espera el backend.
export const fromDateTimeInput = (value) => `${value}:00`
export const fromDateInput = (value) => `${value}T00:00:00`

// Fecha-hora local actual sin zona horaria (para que la IA resuelva "mañana", "el martes", etc.).
export const toLocalNowIso = (date = new Date()) => `${toDateKey(date)}T${pad(date.getHours())}:${pad(date.getMinutes())}:00`
