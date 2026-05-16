import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { login } from '../api/authApi'
import { useAuth } from '../features/auth/AuthContext'

type LoginForm = {
  username: string
  password: string
}

export function LoginPage() {
  const { register, handleSubmit } = useForm<LoginForm>()
  const { login: authLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)

  const onSubmit = async (data: LoginForm) => {
    try {
      setError(null)
      const response = await login(data.username, data.password)
      authLogin(response.token)
      const redirectPath = (location.state as { from?: string } | null)?.from ?? '/tours/me'
      navigate(redirectPath)
    } catch {
      setError('Login failed. Check credentials.')
    }
  }

  return (
    <section>
      <h1>Login</h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <input {...register('username', { required: true })} placeholder="Username" />
        <input {...register('password', { required: true })} placeholder="Password" type="password" />
        <button type="submit">Login</button>
      </form>
      {error && <p>{error}</p>}
      <p>
        No account yet? <Link to="/register">Register</Link>
      </p>
    </section>
  )
}
