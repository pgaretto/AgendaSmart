import { useEffect, useRef, useState } from 'react'
import { api } from '../services/api'
import { isRecoverable } from '../services/outbox'
import {
  fromDateInput,
  fromDateTimeInput,
  toDateInput,
  toDateKey,
  toDateTimeInput,
} from '../utils/calendar'

const CATEGORIES = ['Comida', 'Transporte', 'Salidas', 'Salud', 'Otros']

function ConfirmModal({ proposal, onClose, onSaved, onNetworkError }) {
  const dialogRef = useRef(null)

  const [event, setEvent] = useState(() =>
    proposal.event ? { title: proposal.event.title, startsAt: toDateTimeInput(proposal.event.startsAt) } : null,
  )
  const [expense, setExpense] = useState(() =>
    proposal.expense
      ? {
          amount: String(proposal.expense.amount),
          category: proposal.expense.category,
          // El gasto cuenta el día del evento; si no hay evento, hoy.
          date: proposal.event ? toDateInput(proposal.event.startsAt) : toDateKey(new Date()),
        }
      : null,
  )
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    const dialog = dialogRef.current
    dialog.showModal()
    return () => dialog.close()
  }, [])

  const handleSubmit = async (e) => {
    e.preventDefault()

    const amount = Number(expense?.amount)
    if (expense && (!Number.isFinite(amount) || amount <= 0)) {
      setError('Ingresá un monto mayor a 0.')
      return
    }

    setSaving(true)
    setError(null)
    const payload = {
      event: event && { title: event.title.trim(), startsAt: fromDateTimeInput(event.startsAt) },
      expense: expense && { amount, category: expense.category, date: fromDateInput(expense.date) },
    }
    try {
      await api.post('/api/entries', payload)
      onSaved()
    } catch (err) {
      if (isRecoverable(err) && onNetworkError) {
        onNetworkError(payload)
        return
      }
      setError('No se pudo guardar. Probá de nuevo.')
      setSaving(false)
    }
  }

  return (
    <dialog ref={dialogRef} className="confirm-modal" aria-labelledby="confirm-title" onCancel={onClose}>
      <form onSubmit={handleSubmit}>
        <h2 id="confirm-title">¿Está todo bien?</h2>

        {event && (
          <fieldset>
            <legend>Evento</legend>
            <label>
              Título
              <input
                type="text"
                value={event.title}
                maxLength={200}
                required
                onChange={(e) => setEvent({ ...event, title: e.target.value })}
              />
            </label>
            <label>
              Fecha y hora
              <input
                type="datetime-local"
                value={event.startsAt}
                required
                onChange={(e) => setEvent({ ...event, startsAt: e.target.value })}
              />
            </label>
            {expense && (
              <button type="button" onClick={() => setEvent(null)}>
                Quitar evento
              </button>
            )}
          </fieldset>
        )}

        {expense && (
          <fieldset>
            <legend>Gasto</legend>
            <label>
              Monto
              <input
                type="number"
                inputMode="decimal"
                min="0.01"
                step="any"
                value={expense.amount}
                required
                onChange={(e) => setExpense({ ...expense, amount: e.target.value })}
              />
            </label>
            <label>
              Categoría
              <select value={expense.category} onChange={(e) => setExpense({ ...expense, category: e.target.value })}>
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Fecha
              <input
                type="date"
                value={expense.date}
                required
                onChange={(e) => setExpense({ ...expense, date: e.target.value })}
              />
            </label>
            {event && (
              <button type="button" onClick={() => setExpense(null)}>
                Quitar gasto
              </button>
            )}
          </fieldset>
        )}

        {error && <p role="alert">{error}</p>}

        <div className="confirm-actions">
          <button type="submit" disabled={saving}>
            Confirmar
          </button>
          <button type="button" onClick={onClose} disabled={saving}>
            Cancelar
          </button>
        </div>
      </form>
    </dialog>
  )
}

export default ConfirmModal
