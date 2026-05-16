import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getMyTours } from '../api/tourApi'
import type { TourResponse } from '../shared/types/tour'

export function MyToursPage() {
  const [tours, setTours] = useState<TourResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const data = await getMyTours()
        setTours(data)
      } catch {
        setError('Failed to load tours.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [])

  if (loading) return <p>Loading tours...</p>

  return (
    <section>
      <h1>My Tours</h1>
      <Link to="/tours/new">Create new tour</Link>
      {error && <p>{error}</p>}
      {tours.length === 0 ? (
        <p>No tours yet.</p>
      ) : (
        <ul>
          {tours.map((tour) => (
            <li key={tour.id}>
              <Link to={`/tours/${tour.id}`}>{tour.name}</Link> ({tour.status})
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
