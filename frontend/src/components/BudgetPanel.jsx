import { useEffect, useState } from 'react'
import { api } from '../services/api'

const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 })

function BudgetPanel() {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth() + 1

  const [budget, setBudget] = useState(null)
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState(false)
  const [amount, setAmount] = useState('')
  const [error, setError] = useState(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let cancelled = false

    api
      .get(`/api/budget?year=${year}&month=${month}`)
      .then((data) => !cancelled && setBudget(data))
      .catch((err) => {
        if (cancelled) return
        if (err.status === 404) setBudget(null)
        else setError('No se pudo cargar el presupuesto.')
      })
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [year, month])

  const startEditing = () => {
    setAmount(budget ? String(budget.amount) : '')
    setError(null)
    setEditing(true)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const value = Number(amount)
    if (!Number.isFinite(value) || value <= 0) {
      setError('Ingresá un monto mayor a 0.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      const body = { year, month, amount: value }
      setBudget(budget ? await api.put('/api/budget', body) : await api.post('/api/budget', body))
      setEditing(false)
    } catch {
      setError('No se pudo guardar el presupuesto.')
    } finally {
      setSaving(false)
    }
  }

  const monthName = now.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' })

  return (
    <section className="budget" aria-label="Presupuesto del mes">
      <h2>Presupuesto de {monthName}</h2>

      {loading ? (
        <p>Cargando…</p>
      ) : (
        <>
          {budget && !editing && (
            <dl className="budget-summary">
              <div>
                <dt>Disponible</dt>
                <dd className={budget.available < 0 ? 'is-negative' : undefined}>
                  {currency.format(budget.available)}
                </dd>
              </div>
              <div>
                <dt>Presupuesto</dt>
                <dd>{currency.format(budget.amount)}</dd>
              </div>
              <div>
                <dt>Gastado</dt>
                <dd>{currency.format(budget.spent)}</dd>
              </div>
            </dl>
          )}

          {!budget && !editing && <p>Todavía no definiste un presupuesto para este mes.</p>}

          {editing ? (
            <form onSubmit={handleSubmit}>
              <label>
                Monto del presupuesto
                <input
                  type="number"
                  inputMode="decimal"
                  min="0.01"
                  step="any"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                />
              </label>
              <div className="budget-actions">
                <button type="submit" disabled={saving}>
                  Guardar
                </button>
                <button type="button" onClick={() => setEditing(false)} disabled={saving}>
                  Cancelar
                </button>
              </div>
            </form>
          ) : (
            <button type="button" onClick={startEditing}>
              {budget ? 'Editar presupuesto' : 'Definir presupuesto'}
            </button>
          )}
        </>
      )}

      {error && <p role="alert">{error}</p>}
    </section>
  )
}

export default BudgetPanel
