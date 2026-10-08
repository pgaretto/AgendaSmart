import { createContext, useContext, useMemo, useState } from 'react'
import { api } from '../services/api'
import { tokenStorage } from '../services/tokenStorage'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => tokenStorage.get())

  const login = async (email, password) => {
    const { token: newToken } = await api.post('/api/auth/login', { email, password })
    tokenStorage.set(newToken)
    setToken(newToken)
  }

  const register = (email, password) => api.post('/api/auth/register', { email, password })

  const logout = () => {
    tokenStorage.clear()
    setToken(null)
  }

  const value = useMemo(
    () => ({ isAuthenticated: Boolean(token), login, register, logout }),
    [token],
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
