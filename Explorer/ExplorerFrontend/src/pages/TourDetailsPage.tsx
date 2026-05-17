import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useParams } from 'react-router-dom'
import { MapContainer, Marker, Polyline, TileLayer, useMapEvents } from 'react-leaflet'
import {
  addKeyPoint,
  addTourReview,
  deleteKeyPoint,
  getTourById,
  getTourReviews,
  updateKeyPoint,
} from '../api/tourApi'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'
import type { TourReviewResponse } from '../shared/types/review'
import type { TourResponse } from '../shared/types/tour'

type KeyPointForm = {
  name: string
  description: string
  latitude: number
  longitude: number
  imageUrl: string
  orderIndex: number
}

type ReviewForm = {
  rating: number
  comment: string
  visitedAtUtc: string
  imageUrlsCsv: string
}

function MapClickHandler({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (event) => onPick(event.latlng.lat, event.latlng.lng),
  })
  return null
}

export function TourDetailsPage() {
  const { id } = useParams()
  const { token } = useAuth()
  const authUser = parseAuthUser(token)
  const [tour, setTour] = useState<TourResponse | null>(null)
  const [reviews, setReviews] = useState<TourReviewResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [keyPointError, setKeyPointError] = useState<string | null>(null)
  const [reviewError, setReviewError] = useState<string | null>(null)
  const [reviewSuccess, setReviewSuccess] = useState<string | null>(null)
  const [editingKeyPointId, setEditingKeyPointId] = useState<number | null>(null)
  const [selectedMapPosition, setSelectedMapPosition] = useState<[number, number] | null>(null)
  const keyPointForm = useForm<KeyPointForm>({
    defaultValues: {
      name: '',
      description: '',
      latitude: 0,
      longitude: 0,
      imageUrl: '',
      orderIndex: 0,
    },
  })
  const reviewForm = useForm<ReviewForm>({
    defaultValues: {
      rating: 5,
      comment: '',
      visitedAtUtc: '',
      imageUrlsCsv: '',
    },
  })

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
        const [data, loadedReviews] = await Promise.all([getTourById(tourId), getTourReviews(tourId)])
        setTour(data)
        setReviews(loadedReviews)
      } catch {
        setError('Failed to load tour details.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [id])

  const sortedKeyPoints = useMemo(() => {
    if (!tour) {
      return []
    }
    return [...tour.keyPoints].sort((a, b) => a.orderIndex - b.orderIndex)
  }, [tour])

  const mapCenter = useMemo<[number, number]>(() => {
    if (selectedMapPosition) {
      return selectedMapPosition
    }
    if (sortedKeyPoints.length > 0) {
      return [sortedKeyPoints[0].latitude, sortedKeyPoints[0].longitude]
    }
    return [44.7866, 20.4489]
  }, [selectedMapPosition, sortedKeyPoints])

  if (loading) return <p>Loading tour details...</p>
  if (error) return <p>{error}</p>
  if (!tour) return <p>Tour not found.</p>

  const canManageKeyPoints = authUser.role === 'Guide' && authUser.userId === tour.authorId
  const canReview = authUser.role === 'Tourist'

  const resetKeyPointForm = () => {
    keyPointForm.reset({
      name: '',
      description: '',
      latitude: 0,
      longitude: 0,
      imageUrl: '',
      orderIndex: tour.keyPoints.length,
    })
    setSelectedMapPosition(null)
    setEditingKeyPointId(null)
  }

  const refreshTourData = async () => {
    const tourId = Number(id)
    const [data, loadedReviews] = await Promise.all([getTourById(tourId), getTourReviews(tourId)])
    setTour(data)
    setReviews(loadedReviews)
  }

  const handleMapPick = (lat: number, lng: number) => {
    keyPointForm.setValue('latitude', lat, { shouldValidate: true })
    keyPointForm.setValue('longitude', lng, { shouldValidate: true })
    setSelectedMapPosition([lat, lng])
  }

  const handleSubmitKeyPoint = keyPointForm.handleSubmit(async (data) => {
    try {
      setKeyPointError(null)
      if (!selectedMapPosition && editingKeyPointId === null) {
        setKeyPointError('Select key point location by clicking on the map.')
        return
      }

      const payload = {
        ...data,
        imageUrl: data.imageUrl.trim() || null,
      }

      if (editingKeyPointId === null) {
        await addKeyPoint(tour.id, payload)
      } else {
        await updateKeyPoint(tour.id, editingKeyPointId, payload)
      }

      await refreshTourData()
      resetKeyPointForm()
    } catch {
      setKeyPointError('Failed to save key point.')
    }
  })

  const handleEditKeyPoint = (keyPoint: TourResponse['keyPoints'][number]) => {
    setEditingKeyPointId(keyPoint.id)
    setSelectedMapPosition([keyPoint.latitude, keyPoint.longitude])
    keyPointForm.reset({
      name: keyPoint.name,
      description: keyPoint.description,
      latitude: keyPoint.latitude,
      longitude: keyPoint.longitude,
      imageUrl: keyPoint.imageUrl ?? '',
      orderIndex: keyPoint.orderIndex,
    })
  }

  const handleDeleteKeyPoint = async (keyPointId: number) => {
    try {
      setKeyPointError(null)
      await deleteKeyPoint(tour.id, keyPointId)
      await refreshTourData()
      if (editingKeyPointId === keyPointId) {
        resetKeyPointForm()
      }
    } catch {
      setKeyPointError('Failed to delete key point.')
    }
  }

  const handleSubmitReview = reviewForm.handleSubmit(async (data) => {
    try {
      setReviewError(null)
      setReviewSuccess(null)

      const visitedDate = new Date(data.visitedAtUtc)
      if (Number.isNaN(visitedDate.getTime())) {
        setReviewError('Visited at date is invalid.')
        return
      }

      if (data.rating < 1 || data.rating > 5) {
        setReviewError('Rating must be between 1 and 5.')
        return
      }

      const imageUrls = data.imageUrlsCsv
        .split(',')
        .map((item) => item.trim())
        .filter((item) => item.length > 0)

      await addTourReview(tour.id, {
        rating: Number(data.rating),
        comment: data.comment.trim(),
        visitedAtUtc: visitedDate.toISOString(),
        imageUrls,
      })

      reviewForm.reset({
        rating: 5,
        comment: '',
        visitedAtUtc: '',
        imageUrlsCsv: '',
      })
      await refreshTourData()
      setReviewSuccess('Review created.')
    } catch {
      setReviewError('Failed to create review.')
    }
  })

  const polylinePositions = sortedKeyPoints.map((keyPoint) => [keyPoint.latitude, keyPoint.longitude] as [number, number])

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

      <h2>Tour Map</h2>
      <div className="map-container">
        <MapContainer center={mapCenter} zoom={13} scrollWheelZoom className="map-view">
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {canManageKeyPoints && <MapClickHandler onPick={handleMapPick} />}
          {polylinePositions.length >= 2 && <Polyline positions={polylinePositions} color="#c2410c" />}
          {sortedKeyPoints.map((keyPoint) => (
            <Marker key={keyPoint.id} position={[keyPoint.latitude, keyPoint.longitude]} />
          ))}
          {selectedMapPosition && <Marker position={selectedMapPosition} />}
        </MapContainer>
      </div>

      <h2>Key Points</h2>
      {sortedKeyPoints.length === 0 ? (
        <p>No key points yet.</p>
      ) : (
        <ul>
          {sortedKeyPoints.map((keyPoint) => (
            <li key={keyPoint.id}>
              <strong>{keyPoint.name}</strong> ({keyPoint.latitude}, {keyPoint.longitude}) - Order {keyPoint.orderIndex}
            </li>
          ))}
        </ul>
      )}

      {canManageKeyPoints && (
        <section>
          <h2>{editingKeyPointId === null ? 'Add Key Point' : 'Edit Key Point'}</h2>
          <p>Click on map to select key point coordinates.</p>
          <form onSubmit={handleSubmitKeyPoint}>
            <label htmlFor="keypoint-name">Name</label>
            <input id="keypoint-name" {...keyPointForm.register('name', { required: true })} placeholder="Key point name" />
            <label htmlFor="keypoint-description">Description</label>
            <textarea
              id="keypoint-description"
              {...keyPointForm.register('description', { required: true })}
              placeholder="Key point description"
            />
            <label htmlFor="keypoint-latitude">Latitude</label>
            <input
              id="keypoint-latitude"
              {...keyPointForm.register('latitude', { required: true, valueAsNumber: true })}
              placeholder="Latitude"
              type="number"
              step="any"
            />
            <label htmlFor="keypoint-longitude">Longitude</label>
            <input
              id="keypoint-longitude"
              {...keyPointForm.register('longitude', { required: true, valueAsNumber: true })}
              placeholder="Longitude"
              type="number"
              step="any"
            />
            <label htmlFor="keypoint-image-url">Image URL (optional)</label>
            <input id="keypoint-image-url" {...keyPointForm.register('imageUrl')} placeholder="Image URL" />
            <label htmlFor="keypoint-order-index">Order Index</label>
            <input
              id="keypoint-order-index"
              {...keyPointForm.register('orderIndex', { required: true, valueAsNumber: true })}
              placeholder="Order index"
              type="number"
            />
            <button type="submit">{editingKeyPointId === null ? 'Add key point' : 'Save changes'}</button>
            {editingKeyPointId !== null && (
              <button type="button" onClick={resetKeyPointForm}>
                Cancel edit
              </button>
            )}
          </form>
          {keyPointError && <p>{keyPointError}</p>}
        </section>
      )}

      {canManageKeyPoints && sortedKeyPoints.length > 0 && (
        <section>
          <h2>Manage Key Points</h2>
          <ul>
            {sortedKeyPoints.map((keyPoint) => (
              <li key={keyPoint.id}>
                <strong>{keyPoint.name}</strong> ({keyPoint.latitude}, {keyPoint.longitude})
                <button type="button" onClick={() => handleEditKeyPoint(keyPoint)}>
                  Edit
                </button>
                <button type="button" onClick={() => void handleDeleteKeyPoint(keyPoint.id)}>
                  Delete
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section>
        <h2>Reviews</h2>
        {reviews.length === 0 ? (
          <p>No reviews yet.</p>
        ) : (
          <ul>
            {reviews.map((review) => (
              <li key={review.id}>
                <strong>{review.touristUsername}</strong> rated {review.rating}/5
                <p>{review.comment}</p>
                <p>Visited: {new Date(review.visitedAtUtc).toLocaleString()}</p>
                <p>Commented: {new Date(review.createdAtUtc).toLocaleString()}</p>
                {review.imageUrls.length > 0 && <p>Images: {review.imageUrls.join(', ')}</p>}
              </li>
            ))}
          </ul>
        )}
      </section>

      {canReview && (
        <section>
          <h2>Add Review</h2>
          <form onSubmit={handleSubmitReview}>
            <input
              {...reviewForm.register('rating', { required: true, valueAsNumber: true, min: 1, max: 5 })}
              type="number"
              min="1"
              max="5"
              placeholder="Rating"
            />
            <textarea {...reviewForm.register('comment', { required: true })} placeholder="Comment" />
            <input
              {...reviewForm.register('visitedAtUtc', { required: true })}
              type="datetime-local"
              placeholder="Visited at"
            />
            <input {...reviewForm.register('imageUrlsCsv')} placeholder="Image URLs (comma-separated)" />
            <button type="submit">Create review</button>
          </form>
          {reviewError && <p>{reviewError}</p>}
          {reviewSuccess && <p>{reviewSuccess}</p>}
        </section>
      )}
    </section>
  )
}
