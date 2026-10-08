// Cola local de envíos fallidos por pérdida de señal (RNF-04).
// - Un envío que falla por red se guarda y se reintenta solo, hasta MAX_ATTEMPTS veces, al volver la conexión.
// - Si agota los reintentos (o el servidor lo rechaza), queda en 'manual' para que el usuario decida.
// Las colas son por usuario para no mezclar datos entre cuentas (RNF-03).

export const MAX_ATTEMPTS = 5
export const BASE_DELAY_MS = 1000

// kind: 'text' (frase pendiente de interpretar) | 'entry' (evento/gasto ya confirmado, pendiente de guardar)
// status: 'queued' | 'ready' (frase interpretada, falta que el usuario la confirme) | 'manual'
export const outboxKey = (userId) => `smart-agenda:outbox:${userId}`

// El id de usuario va en el claim `sub` del JWT; sirve solo para separar las colas locales, no para autorizar.
export function userIdFromToken(token) {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(payload)).sub ?? null
  } catch {
    return null
  }
}

export function loadItems(storage, key) {
  try {
    const items = JSON.parse(storage.getItem(key) ?? '[]')
    return Array.isArray(items) ? items : []
  } catch {
    return []
  }
}

export function saveItems(storage, key, items) {
  try {
    storage.setItem(key, JSON.stringify(items))
  } catch {
    // Sin espacio o storage bloqueado: la cola sigue viva en memoria durante la sesión.
  }
}

export const createItem = (kind, payload) => ({
  id: crypto.randomUUID(),
  kind,
  payload,
  attempts: 0,
  status: 'queued',
  createdAt: new Date().toISOString(),
})

export const backoffDelay = (attempt) => BASE_DELAY_MS * 2 ** (attempt - 1)

/**
 * Reintenta un envío hasta agotar MAX_ATTEMPTS. Devuelve el item actualizado:
 *  - éxito: { status: 'ready', result } para 'text'; { status: 'done' } para 'entry'.
 *  - falla de red tras MAX_ATTEMPTS, o rechazo del servidor: { status: 'manual' }.
 * `send(item)` hace la llamada real; `onProgress(item)` permite persistir cada intento.
 */
export async function retryItem(item, { send, wait, onProgress = () => {}, maxAttempts = MAX_ATTEMPTS }) {
  let current = { ...item, status: 'queued' }

  while (current.attempts < maxAttempts) {
    current = { ...current, attempts: current.attempts + 1 }

    try {
      const result = await send(current)
      return current.kind === 'text'
        ? { ...current, status: 'ready', result }
        : { ...current, status: 'done' }
    } catch (error) {
      if (!error.isNetwork) {
        return { ...current, status: 'manual', error: error.status ?? 'rechazado' }
      }
      onProgress(current)
      if (current.attempts < maxAttempts) await wait(backoffDelay(current.attempts))
    }
  }

  return { ...current, status: 'manual', error: 'sin-conexion' }
}
