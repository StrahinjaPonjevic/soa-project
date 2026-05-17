import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getTours } from '../api/tourApi'
import type { TourResponse } from '../shared/types/tour'

export function ToursCatalogPage() {
  const [tours, setTours] = useState<TourResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const data = await getTours()
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
    <section className="tours-catalog">
      <h1 className="tours-catalog-title">All Tours</h1>
      {error && <p>{error}</p>}
      {tours.length === 0 ? (
        <p>No tours available yet.</p>
      ) : (
        <ul className="tour-cards">
          {tours.map((tour) => (
            <li key={tour.id} className="tour-card">
              <Link className="tour-card-title" to={`/tours/${tour.id}`}>
                {tour.name}
              </Link>
              <p className="tour-card-meta">
                by <strong>{tour.authorUsername}</strong>
              </p>
              <p className="tour-card-meta">
                Difficulty: <strong>{tour.difficulty}</strong>
              </p>
              <p className="tour-card-meta">
                Status: <strong>{tour.status}</strong> | Price: <strong>{tour.price}</strong>
              </p>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
