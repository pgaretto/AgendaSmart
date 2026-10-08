import { useState } from 'react'
import { api } from '../services/api'
import { toLocalNowIso } from '../utils/calendar'
import ConfirmModal from './ConfirmModal'

function EntryInput({ onSaved, enqueue }) {
  const [text, setText] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [proposal, setProposal] = useState(null)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const trimmed = text.trim()
    if (!trimmed) return

    setLoading(true)
    setError(null)
    const request = { text: trimmed, clientNow: toLocalNowIso() }
    try {
      const result = await api.post('/api/parse', request)
      if (!result.event && !result.expense) {
        setError('No encontré ningún evento ni gasto en esa frase. Probá con más detalle.')
      } else {
        setProposal(result)
      }
    } catch (err) {
      if (err.isNetwork) {
        // Sin señal: la frase queda guardada y se envía sola al volver la conexión (RNF-04).
        enqueue('text', request)
        setText('')
      } else {
        setError('No se pudo interpretar el texto. Probá de nuevo.')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSaved = () => {
    setProposal(null)
    setText('')
    onSaved?.()
  }

  return (
    <section className="entry" aria-label="Cargar evento o gasto">
      <form onSubmit={handleSubmit}>
        <label>
          ¿Qué querés anotar?
          <input
            type="text"
            value={text}
            maxLength={500}
            placeholder="Mañana almuerzo con mamá y gasto 15000"
            onChange={(e) => setText(e.target.value)}
            disabled={loading}
          />
        </label>
        <button type="submit" disabled={loading || !text.trim()}>
          {loading ? 'Interpretando…' : 'Enviar'}
        </button>
      </form>
      {error && <p role="alert">{error}</p>}

      {proposal && (
        <ConfirmModal
          proposal={proposal}
          onClose={() => setProposal(null)}
          onSaved={handleSaved}
          onNetworkError={(payload) => {
            enqueue('entry', payload)
            setProposal(null)
            setText('')
          }}
        />
      )}
    </section>
  )
}

export default EntryInput
