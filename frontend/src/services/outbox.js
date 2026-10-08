// Cola local de envíos fallidos por pérdida de señal (RNF-04).
// - Un envío que falla por red se guarda y se reintenta solo, hasta MAX_ATTEMPTS veces, al volver la conexión.
// - Si agota los reintentos (o el servidor lo rechaza), queda en 'manual' para que el usuario decida.
// Las colas son por usuario para no mezclar datos entre cuentas (RNF-03).

import { decodePayload } from './jwt'

export const MAX_ATTEMPTS = 5
export const BASE_DELAY_MS = 1000

// kind: 'text' (frase pendiente de interpretar) | 'entry' (evento/gasto ya confirmado, pendiente de guardar)
// error: 'sesion' (pausado: la sesión venció, sigue al volver a iniciar sesión) | 'sin-conexion' | código HTTP
// status: 'queued' | 'ready' (frase interpretada, falta que el usuario la confirme) | 'manual'
export const outboxKey = (userId) => `smart-agenda:outbox:${userId}`

// El id de usuario va en el claim `sub` del JWT; sirve solo para separar las colas locales, no para autorizar.
export const userIdFromToken = (token) => decodePayload(token)?.sub ?? null

// Falla que no es culpa del envío: sin señal o sesión vencida. Se guarda para reintentar en vez de perderlo.
export const isRecoverable = (error) => Boolean(error.isNetwork) || error.status === 401

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
 *  - sesión vencida (401): { status: 'queued', error: 'sesion' }, sin gastar intentos.
 * `send(item)` hace la llamada real; `onProgress(item)` permite persistir cada intento.
 */
export async function retryItem(item, { send, wait, onProgress = () => {}, maxAttempts = MAX_ATTEMPTS }) {
  let current = { ...item, status: 'queued', error: undefined }

  while (current.attempts < maxAttempts) {
    current = { ...current, attempts: current.attempts + 1 }

    try {
      const result = await send(current)
      return current.kind === 'text'
        ? { ...current, status: 'ready', result }
        : { ...current, status: 'done' }
    } catch (error) {
      if (error.status === 401) {
        // Sesión vencida: no cuenta como intento fallido; queda en pausa hasta volver a iniciar sesión.
        return { ...current, attempts: current.attempts - 1, status: 'queued', error: 'sesion' }
      }
      if (!error.isNetwork) {
        return { ...current, status: 'manual', error: error.status ?? 'rechazado' }
      }
      onProgress(current)
      if (current.attempts < maxAttempts) await wait(backoffDelay(current.attempts))
    }
  }

  return { ...current, status: 'manual', error: 'sin-conexion' }
}
