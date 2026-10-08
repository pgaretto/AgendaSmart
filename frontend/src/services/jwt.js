// Lectura local del JWT. Sirve para decidir qué mostrar, nunca para autorizar: eso lo valida el backend.

export function decodePayload(token) {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(payload))
  } catch {
    return null
  }
}

// Un token ilegible o sin `exp` se considera vencido.
export function isExpired(token, now = Date.now()) {
  const exp = decodePayload(token)?.exp
  return typeof exp !== 'number' || exp * 1000 <= now
}
