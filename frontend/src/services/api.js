import { tokenStorage } from './tokenStorage'

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5222'

async function request(path, options = {}) {
  const token = tokenStorage.get()

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })

  const isJson = response.headers.get('content-type')?.includes('application/json')
  const body = isJson ? await response.json() : null

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
