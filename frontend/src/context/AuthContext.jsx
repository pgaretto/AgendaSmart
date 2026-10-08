import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { api, setUnauthorizedHandler } from '../services/api'
import { isExpired } from '../services/jwt'
import { tokenStorage } from '../services/tokenStorage'

const AuthContext = createContext(null)

// Un token vencido que quedó guardado se descarta al abrir la app.
const loadToken = () => {
  const stored = tokenStorage.get()
  if (stored && isExpired(stored)) {
    tokenStorage.clear()
    return { token: null, expired: true }
  }
  return { token: stored, expired: false }
}

export function AuthProvider({ children }) {
  const [initial] = useState(loadToken)
  const [token, setToken] = useState(initial.token)
  const [sessionExpired, setSessionExpired] = useState(initial.expired)

  const login = async (email, password) => {
    const { token: newToken } = await api.post('/api/auth/login', { email, password })
    tokenStorage.set(newToken)
    setSessionExpired(false)
    setToken(newToken)
  }

  const register = (email, password) => api.post('/api/auth/register', { email, password })

  const logout = () => {
    tokenStorage.clear()
    setSessionExpired(false)
    setToken(null)
  }

  const expireSession = useCallback(() => {
    tokenStorage.clear()
    setSessionExpired(true)
    setToken(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(expireSession)
    return () => setUnauthorizedHandler(null)
  }, [expireSession])

  const value = useMemo(
    () => ({ isAuthenticated: Boolean(token), sessionExpired, login, register, logout }),
    [token, sessionExpired],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth debe usarse dentro de un AuthProvider')
  }
  return context
}
