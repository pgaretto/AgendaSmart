import { useEffect, useMemo, useState } from 'react'
import { api } from '../services/api'
import { addDays, getMonthGrid, getWeek, parseDateKey, toDateKey, toLocalIso } from '../utils/calendar'

const WEEKDAYS = ['L', 'M', 'X', 'J', 'V', 'S', 'D']

const formatTime = (date) =>
  date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit', hour12: false })

const formatShort = (date) => date.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })

function Calendar({ refreshKey = 0 }) {
  const today = new Date()
  const [view, setView] = useState('month')
  const [selected, setSelected] = useState(toDateKey(today))
  // Fecha de referencia del período visible (mes o semana).
  const [anchor, setAnchor] = useState(today)
  const [events, setEvents] = useState([])
  const [error, setError] = useState(null)

  const isMonth = view === 'month'
  const weeks = useMemo(
    () => (isMonth ? getMonthGrid(anchor.getFullYear(), anchor.getMonth()) : [getWeek(anchor)]),
    [isMonth, anchor],
  )

  useEffect(() => {
    const first = weeks[0][0]
    const to = addDays(weeks[weeks.length - 1][6], 1)
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
  }, [weeks, refreshKey])

  const eventsByDay = useMemo(() => {
    const map = new Map()
    for (const event of events) {
      const startsAt = new Date(event.startsAt)
      const key = toDateKey(startsAt)
      map.set(key, [...(map.get(key) ?? []), { ...event, startsAt }])
    }
    return map
  }, [events])

  const step = (delta) =>
    setAnchor((a) => (isMonth ? new Date(a.getFullYear(), a.getMonth() + delta, 1) : addDays(a, delta * 7)))

  const changeView = (next) => {
    setView(next)
    // Mantiene el día elegido a la vista al cambiar de modo.
    setAnchor(parseDateKey(selected))
  }

  const title = isMonth
    ? anchor.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' })
    : `${formatShort(weeks[0][0])} – ${formatShort(weeks[0][6])} ${weeks[0][6].getFullYear()}`
  const selectedEvents = eventsByDay.get(selected) ?? []
  const todayKey = toDateKey(today)

  return (
    <section className="calendar" aria-label={isMonth ? 'Calendario mensual' : 'Calendario semanal'}>
      <div className="calendar-toggle" role="group" aria-label="Vista del calendario">
        <button type="button" aria-pressed={isMonth} onClick={() => changeView('month')}>
          Mes
        </button>
        <button type="button" aria-pressed={!isMonth} onClick={() => changeView('week')}>
          Semana
        </button>
      </div>

      <header className="calendar-header">
        <button type="button" onClick={() => step(-1)} aria-label={isMonth ? 'Mes anterior' : 'Semana anterior'}>
          ‹
        </button>
        <h2>{title}</h2>
        <button type="button" onClick={() => step(1)} aria-label={isMonth ? 'Mes siguiente' : 'Semana siguiente'}>
          ›
        </button>
      </header>

      {error && <p role="alert">{error}</p>}

      {isMonth ? (
        <>
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
                day.getMonth() !== anchor.getMonth() && 'is-outside',
                key === todayKey && 'is-today',
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
        </>
      ) : (
        <ol className="calendar-week">
          {weeks[0].map((day) => {
            const key = toDateKey(day)
            const dayEvents = eventsByDay.get(key) ?? []
            return (
              <li key={key} className={key === todayKey ? 'is-today' : undefined}>
                <h3>
                  {day.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'short' })}
                </h3>
                {dayEvents.length === 0 ? (
                  <p className="calendar-empty">Sin eventos</p>
                ) : (
                  <ul className="calendar-events">
                    {dayEvents.map((e) => (
                      <li key={e.id}>
                        <strong>{formatTime(e.startsAt)}</strong> {e.title}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            )
          })}
        </ol>
      )}
    </section>
  )
}

export default Calendar
