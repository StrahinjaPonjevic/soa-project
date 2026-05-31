import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getTours, startTour, getActiveTourExecution } from '../api/tourApi'
import type { TourResponse } from '../shared/types/tour'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

export function ToursCatalogPage() {
  const { token } = useAuth()
  const authUser = parseAuthUser(token)
  const navigate = useNavigate()

  const [tours, setTours] = useState<TourResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [startingId, setStartingId] = useState<number | null>(null)
  const [startError, setStartError] = useState<string | null>(null)

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

  const handleStartTour = async (tourId: number) => {
    setStartError(null)
    setStartingId(tourId)
    try {
      const execution = await startTour(tourId)
      navigate(`/tours/executions/${execution.id}`)
    } catch (err: any) {
      if (err?.response?.status === 409) {
        // Already has an active execution — navigate directly to it
        try {
          const active = await getActiveTourExecution()
          navigate(`/tours/executions/${active.id}`)
        } catch {
          // Fallback: navigate to a dummy ID, ActiveTourPage will redirect to real active one
          navigate('/tours/executions/0')
        }
      } else {
        const msg = err?.response?.data?.message ?? 'Failed to start tour.'
        setStartError(msg)
      }
    } finally {
      setStartingId(null)
    }
  }

  if (loading) return <p>Loading tours...</p>

  return (
    <section className="tours-catalog">
      <h1 className="tours-catalog-title">All Tours</h1>
      {error && <p style={{ color: 'var(--danger)' }}>{error}</p>}
      {startError && <p style={{ color: 'var(--danger)' }}>{startError}</p>}
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
              {authUser.role === 'Tourist' && (
                <button
                  type="button"
                  onClick={() => void handleStartTour(tour.id)}
                  disabled={startingId === tour.id}
                  style={{ marginTop: '0.5rem', background: 'var(--accent)' }}
                >
                  {startingId === tour.id ? 'Starting...' : 'Start Tour'}
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
