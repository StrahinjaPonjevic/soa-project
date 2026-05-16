import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { register as registerUser } from '../api/authApi'

type RegisterForm = {
  username: string
  email: string
  password: string
  role: 'Guide' | 'Tourist'
}

export function RegisterPage() {
  const { register, handleSubmit } = useForm<RegisterForm>({
    defaultValues: { role: 'Tourist' },
  })
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const onSubmit = async (data: RegisterForm) => {
    try {
      setError(null)
      await registerUser(data)
      navigate('/login')
    } catch {
      setError('Registration failed. Try another username/email.')
    }
  }

  return (
    <section>
      <h1>Register</h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <input {...register('username', { required: true })} placeholder="Username" />
        <input {...register('email', { required: true })} placeholder="Email" type="email" />
        <input {...register('password', { required: true })} placeholder="Password" type="password" />
        <select {...register('role', { required: true })}>
          <option value="Tourist">Tourist</option>
          <option value="Guide">Guide</option>
        </select>
        <button type="submit">Create account</button>
      </form>
      {error && <p>{error}</p>}
      <p>
        Already have an account? <Link to="/login">Login</Link>
      </p>
    </section>
  )
}
