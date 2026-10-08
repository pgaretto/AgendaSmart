import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const { login, sessionExpired } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(err.status === 401 ? 'Email o contraseña incorrectos.' : 'No pudimos iniciar sesión.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main>
      <h1>Iniciar sesión</h1>
      {sessionExpired && (
        <p role="status">Tu sesión venció. Iniciá sesión de nuevo; lo que estabas enviando se guardó y se reenviará solo.</p>
      )}
      <form onSubmit={handleSubmit}>
        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </label>
        <label>
          Contraseña
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={isSubmitting}>
          Entrar
        </button>
      </form>
      <p>
        ¿No tenés cuenta? <Link to="/register">Registrate</Link>
      </p>
    </main>
  )
}

export default Login
