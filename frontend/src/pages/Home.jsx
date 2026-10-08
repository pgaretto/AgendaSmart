import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import BudgetPanel from '../components/BudgetPanel'
import Calendar from '../components/Calendar'
import EntryInput from '../components/EntryInput'
import { api } from '../services/api'

function Home() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState(null)
  // Se incrementa al guardar para que el calendario y el presupuesto se vuelvan a cargar.
  const [refreshKey, setRefreshKey] = useState(0)

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
    <main className="wide">
      <h1>Smart-Agenda</h1>
      <p>Calendario y gastos en una sola pantalla.</p>
      {email && <p>Sesión iniciada como {email}</p>}
      <EntryInput onSaved={() => setRefreshKey((k) => k + 1)} />
      <BudgetPanel refreshKey={refreshKey} />
      <Calendar refreshKey={refreshKey} />
      <button type="button" onClick={handleLogout}>
        Cerrar sesión
      </button>
    </main>
  )
}

export default Home
