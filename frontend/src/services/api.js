import { tokenStorage } from './tokenStorage'

let onUnauthorized = null

// Lo registra AuthProvider para cerrar la sesión cuando el backend rechaza el token.
export const setUnauthorizedHandler = (handler) => {
  onUnauthorized = handler
}

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5222'

async function request(path, options = {}) {
  const token = tokenStorage.get()

  let response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers,
      },
    })
  } catch (cause) {
    // fetch solo rechaza por fallas de red (sin señal, servidor inalcanzable), no por códigos HTTP.
    const error = new Error('Sin conexión con el servidor.', { cause })
    error.isNetwork = true
    throw error
  }

  const isJson = response.headers.get('content-type')?.includes('application/json')
  const body = isJson ? await response.json() : null

  // 401 con token enviado = sesión vencida o inválida (un login fallido no manda token).
  if (response.status === 401 && token) onUnauthorized?.()

  if (!response.ok) {
    const error = new Error(body?.message ?? `Error ${response.status} al llamar a ${path}`)
    error.status = response.status
    throw error
  }

  return body
}

export const api = {
  get: (path) => request(path),
  post: (path, body) => request(path, { method: 'POST', body: JSON.stringify(body) }),
  put: (path, body) => request(path, { method: 'PUT', body: JSON.stringify(body) }),
}
