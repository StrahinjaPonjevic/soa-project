import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { createTour } from '../api/tourApi'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

type CreateTourForm = {
  name: string
  description: string
  difficulty: string
  tagsCsv: string
}

export function CreateTourPage() {
  const { register, handleSubmit } = useForm<CreateTourForm>()
  const navigate = useNavigate()
  const { token } = useAuth()
  const authUser = parseAuthUser(token)
  const [error, setError] = useState<string | null>(null)

  if (authUser.role !== 'Guide') {
    return (
      <section>
        <h1>Create Tour</h1>
        <p>Only guide accounts can create tours.</p>
      </section>
    )
  }

  const onSubmit = async (data: CreateTourForm) => {
    try {
      setError(null)
      const tags = data.tagsCsv
        .split(',')
        .map((x) => x.trim())
        .filter((x) => x.length > 0)

      const created = await createTour({
        name: data.name,
        description: data.description,
        difficulty: data.difficulty,
        tags,
      })

      navigate(`/tours/${created.id}`)
    } catch {
      setError('Failed to create tour.')
    }
  }

  return (
    <section>
      <h1>Create Tour</h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <input {...register('name', { required: true })} placeholder="Tour name" />
        <textarea {...register('description', { required: true })} placeholder="Description" />
        <input {...register('difficulty', { required: true })} placeholder="Difficulty" />
        <input {...register('tagsCsv')} placeholder="Tags (comma-separated)" />
        <button type="submit">Create</button>
      </form>
      {error && <p>{error}</p>}
    </section>
  )
}
