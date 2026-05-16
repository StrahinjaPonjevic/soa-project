import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getTourById } from '../api/tourApi'
import type { TourResponse } from '../shared/types/tour'

export function TourDetailsPage() {
  const { id } = useParams()
  const [tour, setTour] = useState<TourResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const tourId = Number(id)
    if (!id || Number.isNaN(tourId)) {
      setError('Invalid tour id.')
      setLoading(false)
      return
    }

    const run = async () => {
      try {
        setError(null)
        const data = await getTourById(tourId)
        setTour(data)
      } catch {
        setError('Failed to load tour details.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [id])

  if (loading) return <p>Loading tour details...</p>
  if (error) return <p>{error}</p>
  if (!tour) return <p>Tour not found.</p>

  return (
    <section>
      <h1>Tour Details</h1>
      <p>
        <strong>Name:</strong> {tour.name}
      </p>
      <p>
        <strong>Description:</strong> {tour.description}
      </p>
      <p>
        <strong>Difficulty:</strong> {tour.difficulty}
      </p>
      <p>
        <strong>Status:</strong> {tour.status} | <strong>Price:</strong> {tour.price}
      </p>
      <p>
        <strong>Tags:</strong> {tour.tags.length > 0 ? tour.tags.join(', ') : 'No tags'}
      </p>

      <h2>Key Points</h2>
      {tour.keyPoints.length === 0 ? (
        <p>No key points yet.</p>
      ) : (
        <ul>
          {tour.keyPoints
            .sort((a, b) => a.orderIndex - b.orderIndex)
            .map((keyPoint) => (
              <li key={keyPoint.id}>
                <strong>{keyPoint.name}</strong> ({keyPoint.latitude}, {keyPoint.longitude}) - Order{' '}
                {keyPoint.orderIndex}
              </li>
            ))}
        </ul>
      )}

      <p>Map/key point editor</p>
    </section>
  )
}
