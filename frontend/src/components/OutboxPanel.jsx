import { useState } from 'react'
import { MAX_ATTEMPTS } from '../services/outbox'
import ConfirmModal from './ConfirmModal'

const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 })

function describe(item) {
  if (item.kind === 'text') return `“${item.payload.text}”`
  const { event, expense } = item.payload
  return [event?.title, expense && currency.format(expense.amount)].filter(Boolean).join(' · ')
}

function statusText(item, online) {
  if (item.status === 'ready') return 'Ya la interpreté. Revisala y confirmala para guardarla.'
  if (item.status === 'manual') {
    return item.error === 'sin-conexion'
      ? `No se pudo enviar tras ${MAX_ATTEMPTS} intentos sin conexión.`
      : 'El servidor no aceptó este envío.'
  }
  if (!online) return 'Sin conexión: se reintentará solo cuando vuelva la señal.'
  return `Reintentando… (${item.attempts}/${MAX_ATTEMPTS})`
}

function OutboxPanel({ outbox, onEntrySaved }) {
  const { items, online, enqueue, discard, retryManually } = outbox
  const [reviewing, setReviewing] = useState(null)

  if (items.length === 0) return null

  const handleNetworkError = (payload) => {
    // Perdió la señal justo al confirmar: se guarda ya confirmado y se envía solo al volver.
    enqueue('entry', payload)
    discard(reviewing.id)
    setReviewing(null)
  }

  const handleSaved = () => {
    discard(reviewing.id)
    setReviewing(null)
    onEntrySaved?.()
  }

  return (
    <section className="outbox" aria-label="Envíos pendientes">
      <h2>Envíos pendientes</h2>
      <ul>
        {items.map((item) => (
          <li key={item.id} role={item.status === 'manual' ? 'alert' : undefined}>
            <strong>{describe(item)}</strong>
            <span>{statusText(item, online)}</span>
            <div className="outbox-actions">
              {item.status === 'ready' && (
                <button type="button" onClick={() => setReviewing(item)}>
                  Revisar
                </button>
              )}
              {item.status === 'manual' && (
                <button type="button" onClick={() => retryManually(item.id)}>
                  Reintentar
                </button>
              )}
              {item.status !== 'queued' && (
                <button type="button" onClick={() => discard(item.id)}>
                  Descartar
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>

      {reviewing && (
        <ConfirmModal
          proposal={reviewing.result}
          onClose={() => setReviewing(null)}
          onSaved={handleSaved}
          onNetworkError={handleNetworkError}
        />
      )}
    </section>
  )
}

export default OutboxPanel
