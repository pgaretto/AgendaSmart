import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../services/api'

function Home() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState(null)

  useEffect(() => {
    api
      .get('/api/auth/me')
      .then((user) => setEmail(user.email))
      .catch(() => setEmail(null))
  }, [])

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <main>
      <h1>Smart-Agenda</h1>
      <p>Calendario y gastos en una sola pantalla.</p>
      {email && <p>Sesión iniciada como {email}</p>}
      <button type="button" onClick={handleLogout}>
        Cerrar sesión
      </button>
    </main>
  )
}

export default Home
