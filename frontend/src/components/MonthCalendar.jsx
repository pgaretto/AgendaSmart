import { useEffect, useMemo, useState } from 'react'
import { api } from '../services/api'
import { getMonthGrid, toDateKey, toLocalIso } from '../utils/calendar'

const WEEKDAYS = ['L', 'M', 'X', 'J', 'V', 'S', 'D']

const formatTime = (date) =>
  date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit', hour12: false })

function MonthCalendar() {
  const today = new Date()
  const [cursor, setCursor] = useState({ year: today.getFullYear(), month: today.getMonth() })
  const [selected, setSelected] = useState(toDateKey(today))
  const [events, setEvents] = useState([])
  const [error, setError] = useState(null)

  const weeks = useMemo(() => getMonthGrid(cursor.year, cursor.month), [cursor])

  useEffect(() => {
    const first = weeks[0][0]
    const lastDay = weeks[weeks.length - 1][6]
    const to = new Date(lastDay.getFullYear(), lastDay.getMonth(), lastDay.getDate() + 1)
    let cancelled = false

    api
      .get(`/api/events?from=${toLocalIso(first)}&to=${toLocalIso(to)}`)
      .then((data) => {
        if (!cancelled) {
          setEvents(data)
          setError(null)
        }
      })
      .catch(() => {
        if (!cancelled) setError('No se pudieron cargar los eventos.')
      })

    return () => {
      cancelled = true
    }
  }, [weeks])

  const eventsByDay = useMemo(() => {
    const map = new Map()
    for (const event of events) {
      const startsAt = new Date(event.startsAt)
      const key = toDateKey(startsAt)
      map.set(key, [...(map.get(key) ?? []), { ...event, startsAt }])
    }
    return map
  }, [events])

  const goToMonth = (delta) => {
    const next = new Date(cursor.year, cursor.month + delta, 1)
    setCursor({ year: next.getFullYear(), month: next.getMonth() })
  }

  const title = new Date(cursor.year, cursor.month, 1).toLocaleDateString('es-AR', {
    month: 'long',
    year: 'numeric',
  })
  const selectedEvents = eventsByDay.get(selected) ?? []

  return (
    <section className="calendar" aria-label="Calendario mensual">
      <header className="calendar-header">
        <button type="button" onClick={() => goToMonth(-1)} aria-label="Mes anterior">
          ‹
        </button>
        <h2>{title}</h2>
        <button type="button" onClick={() => goToMonth(1)} aria-label="Mes siguiente">
          ›
        </button>
      </header>

      {error && <p role="alert">{error}</p>}

      <div className="calendar-grid" role="grid">
        {WEEKDAYS.map((d) => (
          <div key={d} className="calendar-weekday" role="columnheader">
            {d}
          </div>
        ))}
        {weeks.flat().map((day) => {
          const key = toDateKey(day)
          const count = eventsByDay.get(key)?.length ?? 0
          const classes = [
            'calendar-day',
            day.getMonth() !== cursor.month && 'is-outside',
            key === toDateKey(today) && 'is-today',
            key === selected && 'is-selected',
          ]
            .filter(Boolean)
            .join(' ')

          return (
            <button
              key={key}
              type="button"
              className={classes}
              onClick={() => setSelected(key)}
              aria-pressed={key === selected}
              aria-label={`${day.getDate()}, ${count} eventos`}
            >
              <span>{day.getDate()}</span>
              {count > 0 && <span className="calendar-dot">{count}</span>}
            </button>
          )
        })}
      </div>

      <h3>Eventos del {selected.split('-').reverse().join('/')}</h3>
      {selectedEvents.length === 0 ? (
        <p>No hay eventos para este día.</p>
      ) : (
        <ul className="calendar-events">
          {selectedEvents.map((e) => (
            <li key={e.id}>
              <strong>{formatTime(e.startsAt)}</strong> {e.title}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export default MonthCalendar
