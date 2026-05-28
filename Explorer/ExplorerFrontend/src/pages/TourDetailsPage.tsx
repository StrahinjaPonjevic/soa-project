import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useParams } from 'react-router-dom'
import { MapContainer, Marker, Polyline, TileLayer, useMapEvents } from 'react-leaflet'
import type { LeafletMouseEvent } from 'leaflet'
import {
  addKeyPoint,
  addTourReview,
  archiveTour,
  deleteKeyPoint,
  getTourById,
  getTourReviews,
  getTourRoutePreview,
  publishTour,
  reactivateTour,
  replaceTravelTimes,
  updateKeyPoint,
  updateTour,
} from '../api/tourApi'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'
import type { TourReviewResponse } from '../shared/types/review'
import type { RoutePointResponse, TourResponse } from '../shared/types/tour'

type TourBasicsForm = {
  name: string
  description: string
  difficulty: string
  tagsCsv: string
}

type KeyPointForm = {
  name: string
  description: string
  latitude: number
  longitude: number
  imageUrl: string
  orderIndex: number
}

type TravelTimesForm = {
  walkingMinutes: string
  bicycleMinutes: string
  carMinutes: string
}

type ReviewForm = {
  rating: number
  comment: string
  visitedAtUtc: string
  imageUrlsCsv: string
}

const LeafletMapContainer = MapContainer as any
const LeafletTileLayer = TileLayer as any
const LeafletPolyline = Polyline as any

function MapClickHandler({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (event: LeafletMouseEvent) => onPick(event.latlng.lat, event.latlng.lng),
  })
  return null
}

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === 'object' && error !== null) {
    const maybeResponse = (error as {
      response?: {
        data?: {
          message?: string
          title?: string
          errors?: string[] | Record<string, string[]>
        }
      }
    }).response
    const message = maybeResponse?.data?.message
    const title = maybeResponse?.data?.title
    const details = maybeResponse?.data?.errors
    if (typeof message === 'string' && message.length > 0) {
      if (Array.isArray(details) && details.length > 0) {
        return `${message} ${details.join(' ')}`
      }

      if (details && typeof details === 'object') {
        const flatDetails = Object.values(details).flat().join(' ')
        return flatDetails.length > 0 ? `${message} ${flatDetails}` : message
      }

      return message
    }

    if (typeof title === 'string' && title.length > 0) {
      if (details && typeof details === 'object' && !Array.isArray(details)) {
        const flatDetails = Object.values(details).flat().join(' ')
        return flatDetails.length > 0 ? `${title} ${flatDetails}` : title
      }

      return title
    }
  }

  return fallback
}

export function TourDetailsPage() {
  const { id } = useParams()
  const { token } = useAuth()
  const authUser = parseAuthUser(token)
  const [tour, setTour] = useState<TourResponse | null>(null)
  const [reviews, setReviews] = useState<TourReviewResponse[]>([])
  const [routePreview, setRoutePreview] = useState<RoutePointResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tourError, setTourError] = useState<string | null>(null)
  const [tourSuccess, setTourSuccess] = useState<string | null>(null)
  const [keyPointError, setKeyPointError] = useState<string | null>(null)
  const [travelTimesError, setTravelTimesError] = useState<string | null>(null)
  const [travelTimesSuccess, setTravelTimesSuccess] = useState<string | null>(null)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [statusSuccess, setStatusSuccess] = useState<string | null>(null)
  const [reviewError, setReviewError] = useState<string | null>(null)
  const [reviewSuccess, setReviewSuccess] = useState<string | null>(null)
  const [editingKeyPointId, setEditingKeyPointId] = useState<number | null>(null)
  const [selectedMapPosition, setSelectedMapPosition] = useState<[number, number] | null>(null)
  const tourBasicsForm = useForm<TourBasicsForm>()
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
  const travelTimesForm = useForm<TravelTimesForm>({
    defaultValues: {
      walkingMinutes: '',
      bicycleMinutes: '',
      carMinutes: '',
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

  const tourId = Number(id)

  const loadTourDetails = async () => {
    if (!id || Number.isNaN(tourId)) {
      setError('Invalid tour id.')
      setLoading(false)
      return
    }

    try {
      setError(null)
      const [loadedTour, loadedReviews, loadedRoutePreview] = await Promise.all([
        getTourById(tourId),
        getTourReviews(tourId),
        getTourRoutePreview(tourId),
      ])
      setTour(loadedTour)
      setReviews(loadedReviews)
      setRoutePreview(loadedRoutePreview)
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to load tour details.'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadTourDetails()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  const sortedKeyPoints = useMemo(() => {
    if (!tour) {
      return []
    }
    return [...tour.keyPoints].sort((a, b) => a.orderIndex - b.orderIndex)
  }, [tour])
  const nextOrderIndex = sortedKeyPoints.length === 0 ? 0 : Math.max(...sortedKeyPoints.map((item) => item.orderIndex)) + 1

  useEffect(() => {
    if (!tour) {
      return
    }

    tourBasicsForm.reset({
      name: tour.name,
      description: tour.description,
      difficulty: tour.difficulty,
      tagsCsv: tour.tags.join(', '),
    })

    travelTimesForm.reset({
      walkingMinutes: String(tour.travelTimes.find((item) => item.transportType === 'Walking')?.durationMinutes ?? ''),
      bicycleMinutes: String(tour.travelTimes.find((item) => item.transportType === 'Bicycle')?.durationMinutes ?? ''),
      carMinutes: String(tour.travelTimes.find((item) => item.transportType === 'Car')?.durationMinutes ?? ''),
    })

    keyPointForm.reset({
      name: '',
      description: '',
      latitude: 0,
      longitude: 0,
      imageUrl: '',
      orderIndex: nextOrderIndex,
    })
  }, [tour, keyPointForm, nextOrderIndex, tourBasicsForm, travelTimesForm])

  const mapCenter = useMemo<[number, number]>(() => {
    if (selectedMapPosition) {
      return selectedMapPosition
    }
    if (routePreview.length > 0) {
      return [routePreview[0].latitude, routePreview[0].longitude]
    }
    if (sortedKeyPoints.length > 0) {
      return [sortedKeyPoints[0].latitude, sortedKeyPoints[0].longitude]
    }
    return [44.7866, 20.4489]
  }, [routePreview, selectedMapPosition, sortedKeyPoints])

  if (loading) return <p>Loading tour details...</p>
  if (error) return <p>{error}</p>
  if (!tour) return <p>Tour not found.</p>

  const canManageTour = authUser.role === 'Guide' && authUser.userId === tour.authorId
  const canReview = authUser.role === 'Tourist' && tour.status === 'Published'
  const routePositions =
    routePreview.length >= 2
      ? routePreview.map((point) => [point.latitude, point.longitude] as [number, number])
      : canManageTour
        ? sortedKeyPoints.map((keyPoint) => [keyPoint.latitude, keyPoint.longitude] as [number, number])
        : []

  const clearMessages = () => {
    setTourError(null)
    setTourSuccess(null)
    setKeyPointError(null)
    setTravelTimesError(null)
    setTravelTimesSuccess(null)
    setStatusError(null)
    setStatusSuccess(null)
  }

  const refreshTourData = async () => {
    await loadTourDetails()
  }

  const resetKeyPointForm = () => {
    keyPointForm.reset({
      name: '',
      description: '',
      latitude: 0,
      longitude: 0,
      imageUrl: '',
      orderIndex: nextOrderIndex,
    })
    setSelectedMapPosition(null)
    setEditingKeyPointId(null)
  }

  const handleMapPick = (lat: number, lng: number) => {
    keyPointForm.setValue('latitude', lat, { shouldValidate: true })
    keyPointForm.setValue('longitude', lng, { shouldValidate: true })
    setSelectedMapPosition([lat, lng])
  }

  const handleSubmitTourBasics = tourBasicsForm.handleSubmit(async (data) => {
    try {
      clearMessages()
      const tags = data.tagsCsv
        .split(',')
        .map((item) => item.trim())
        .filter((item) => item.length > 0)

      const updated = await updateTour(tour.id, {
        name: data.name,
        description: data.description,
        difficulty: data.difficulty,
        tags,
      })

      setTour(updated)
      setTourSuccess('Tour details updated.')
    } catch (err) {
      setTourError(getErrorMessage(err, 'Failed to update tour details.'))
    }
  })

  const handleSubmitKeyPoint = keyPointForm.handleSubmit(async (data) => {
    try {
      clearMessages()
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
    } catch (err) {
      setKeyPointError(getErrorMessage(err, 'Failed to save key point.'))
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
      clearMessages()
      await deleteKeyPoint(tour.id, keyPointId)
      await refreshTourData()
      if (editingKeyPointId === keyPointId) {
        resetKeyPointForm()
      }
    } catch (err) {
      setKeyPointError(getErrorMessage(err, 'Failed to delete key point.'))
    }
  }

  const handleSubmitTravelTimes = travelTimesForm.handleSubmit(async (data) => {
    try {
      clearMessages()
      const entries = [
        { transportType: 'Walking' as const, durationMinutes: Number(data.walkingMinutes) },
        { transportType: 'Bicycle' as const, durationMinutes: Number(data.bicycleMinutes) },
        { transportType: 'Car' as const, durationMinutes: Number(data.carMinutes) },
      ].filter((entry) => Number.isFinite(entry.durationMinutes) && entry.durationMinutes > 0)

      if (entries.length === 0) {
        setTravelTimesError('Define at least one travel time.')
        return
      }

      await replaceTravelTimes(tour.id, {
        travelTimes: entries,
      })
      await refreshTourData()
      setTravelTimesSuccess('Travel times updated.')
    } catch (err) {
      setTravelTimesError(getErrorMessage(err, 'Failed to save travel times.'))
    }
  })

  const handleStatusAction = async (action: 'publish' | 'archive' | 'reactivate') => {
    try {
      clearMessages()
      let updated: TourResponse
      if (action === 'publish') {
        updated = await publishTour(tour.id)
        setStatusSuccess('Tour published.')
      } else if (action === 'archive') {
        updated = await archiveTour(tour.id)
        setStatusSuccess('Tour archived.')
      } else {
        updated = await reactivateTour(tour.id)
        setStatusSuccess('Tour reactivated.')
      }

      setTour(updated)
      await refreshTourData()
    } catch (err) {
      setStatusError(getErrorMessage(err, `Failed to ${action} tour.`))
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
    } catch (err) {
      setReviewError(getErrorMessage(err, 'Failed to create review.'))
    }
  })

  return (
    <section className="tour-details-page">
      <div className="tour-hero card">
        <div>
          <p className="section-eyebrow">Tour details</p>
          <h1>{tour.name}</h1>
          <p className="tour-hero-description">{tour.description}</p>
        </div>
        <div className="tour-badges">
          <span className="pill pill-strong">{tour.status}</span>
          <span className="pill">{tour.difficulty}</span>
          <span className="pill">{tour.lengthKm.toFixed(2)} km</span>
          <span className="pill">Price {tour.price}</span>
        </div>
      </div>

      <div className="tour-layout">
        <div className="tour-main-column">
          <section className="card tour-map-card">
            <div className="section-header">
              <div>
                <p className="section-eyebrow">Route preview</p>
                <h2>Tour Map</h2>
              </div>
              <p className="section-note">
                {routePreview.length >= 2
                  ? 'The line follows the routed path between key points.'
                  : canManageTour
                    ? 'Add at least two key points to generate a routed path.'
                    : 'Public view shows only the first key point.'}
              </p>
            </div>

            <div className="map-container">
              <LeafletMapContainer center={mapCenter} zoom={13} scrollWheelZoom className="map-view">
                <LeafletTileLayer
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                />
                {canManageTour && <MapClickHandler onPick={handleMapPick} />}
                {routePositions.length >= 2 && (
                  <LeafletPolyline positions={routePositions} pathOptions={{ color: '#c2410c', weight: 5, opacity: 0.9 }} />
                )}
                {sortedKeyPoints.map((keyPoint) => (
                  <Marker key={keyPoint.id} position={[keyPoint.latitude, keyPoint.longitude]} />
                ))}
                {selectedMapPosition && <Marker position={selectedMapPosition} />}
              </LeafletMapContainer>
            </div>
          </section>

          <section className="card">
            <div className="section-header">
              <div>
                <p className="section-eyebrow">Waypoints</p>
                <h2>Key Points</h2>
              </div>
              <p className="section-note">{sortedKeyPoints.length} defined point(s)</p>
            </div>

            {sortedKeyPoints.length === 0 ? (
              <p className="empty-state">No key points yet.</p>
            ) : (
              <div className="keypoints-list">
                {sortedKeyPoints.map((keyPoint) => (
                  <article key={keyPoint.id} className="keypoint-card">
                    <div className="keypoint-head">
                      <div>
                        <p className="keypoint-order">Stop {keyPoint.orderIndex + 1}</p>
                        <h3>{keyPoint.name}</h3>
                      </div>
                      {canManageTour && (
                        <div className="inline-actions">
                          <button type="button" className="button-secondary" onClick={() => handleEditKeyPoint(keyPoint)}>
                            Edit
                          </button>
                          <button type="button" className="button-danger" onClick={() => void handleDeleteKeyPoint(keyPoint.id)}>
                            Delete
                          </button>
                        </div>
                      )}
                    </div>
                    <p className="keypoint-description">{keyPoint.description}</p>
                    <p className="keypoint-coordinates">
                      {keyPoint.latitude.toFixed(6)}, {keyPoint.longitude.toFixed(6)}
                    </p>
                  </article>
                ))}
              </div>
            )}
            {keyPointError && <p className="message-error">{keyPointError}</p>}
          </section>

          <section className="card compact-card">
            <div className="section-header">
              <div>
                <p className="section-eyebrow">Reviews</p>
                <h2>Tourist Feedback</h2>
              </div>
            </div>

            {reviews.length === 0 ? (
              <p className="empty-state">No reviews yet.</p>
            ) : (
              <div className="reviews-list">
                {reviews.map((review) => (
                  <article key={review.id} className="review-card">
                    <div className="review-head">
                      <strong>{review.touristUsername}</strong>
                      <span className="pill">{review.rating}/5</span>
                    </div>
                    <p>{review.comment}</p>
                    <p className="review-meta">Visited: {new Date(review.visitedAtUtc).toLocaleString()}</p>
                    <p className="review-meta">Commented: {new Date(review.createdAtUtc).toLocaleString()}</p>
                    {review.imageUrls.length > 0 && <p className="review-meta">Images: {review.imageUrls.join(', ')}</p>}
                  </article>
                ))}
              </div>
            )}
          </section>

          {canReview && (
            <section className="card compact-card">
              <div className="section-header">
                <div>
                  <p className="section-eyebrow">Add feedback</p>
                  <h2>Write a Review</h2>
                </div>
              </div>

              <form className="stacked-form" onSubmit={handleSubmitReview}>
                <div className="form-grid compact-grid">
                  <label>
                    <span>Rating</span>
                    <input
                      {...reviewForm.register('rating', { required: true, valueAsNumber: true, min: 1, max: 5 })}
                      type="number"
                      min="1"
                      max="5"
                      placeholder="1-5"
                    />
                  </label>
                  <label>
                    <span>Visited at</span>
                    <input
                      {...reviewForm.register('visitedAtUtc', { required: true })}
                      type="datetime-local"
                      placeholder="Visited at"
                    />
                  </label>
                </div>
                <label>
                  <span>Comment</span>
                  <textarea {...reviewForm.register('comment', { required: true })} placeholder="Share your experience" />
                </label>
                <label>
                  <span>Image URLs</span>
                  <input {...reviewForm.register('imageUrlsCsv')} placeholder="Comma-separated image URLs" />
                </label>
                <button type="submit">Create review</button>
              </form>
              {reviewError && <p className="message-error">{reviewError}</p>}
              {reviewSuccess && <p className="message-success">{reviewSuccess}</p>}
            </section>
          )}
        </div>

        <aside className="tour-sidebar">
          <section className="card control-panel">
            <div className="section-header">
              <div>
                <p className="section-eyebrow">Summary</p>
                <h2>Overview</h2>
              </div>
            </div>

            <dl className="stats-list compact-stats">
              <div>
                <dt>Status</dt>
                <dd>{tour.status}</dd>
              </div>
              <div>
                <dt>Difficulty</dt>
                <dd>{tour.difficulty}</dd>
              </div>
              <div>
                <dt>Length</dt>
                <dd>{tour.lengthKm.toFixed(2)} km</dd>
              </div>
              <div>
                <dt>Published</dt>
                <dd>{tour.publishedAtUtc ? new Date(tour.publishedAtUtc).toLocaleString() : 'Not published'}</dd>
              </div>
              <div>
                <dt>Archived</dt>
                <dd>{tour.archivedAtUtc ? new Date(tour.archivedAtUtc).toLocaleString() : 'Not archived'}</dd>
              </div>
            </dl>

            <div className="tags-row">
              {tour.tags.length > 0 ? tour.tags.map((tag) => <span key={tag} className="pill">{tag}</span>) : <span className="empty-inline">No tags</span>}
            </div>

            <div className="travel-times-list travel-times-compact">
              {tour.travelTimes.length === 0 ? (
                <p className="empty-state">No travel times defined yet.</p>
              ) : (
                tour.travelTimes.map((travelTime) => (
                  <div key={travelTime.transportType} className="travel-time-row">
                    <span>{travelTime.transportType}</span>
                    <strong>{travelTime.durationMinutes} min</strong>
                  </div>
                ))
              )}
            </div>

            {canManageTour && (
              <div className="status-actions">
                {tour.status === 'Draft' && (
                  <button type="button" onClick={() => void handleStatusAction('publish')}>
                    Publish
                  </button>
                )}
                {tour.status === 'Published' && (
                  <button type="button" className="button-secondary" onClick={() => void handleStatusAction('archive')}>
                    Archive
                  </button>
                )}
                {tour.status === 'Archived' && (
                  <button type="button" onClick={() => void handleStatusAction('reactivate')}>
                    Reactivate
                  </button>
                )}
              </div>
            )}
            {statusError && <p className="message-error">{statusError}</p>}
            {statusSuccess && <p className="message-success">{statusSuccess}</p>}
          </section>

          {canManageTour && (
            <section className="card control-panel">
              <details className="control-section" open>
                <summary>
                  <span>Edit core data</span>
                  <strong>Tour basics</strong>
                </summary>
                <form className="stacked-form compact-form" onSubmit={handleSubmitTourBasics}>
                  <label>
                    <span>Name</span>
                    <input {...tourBasicsForm.register('name', { required: true })} placeholder="Tour name" />
                  </label>
                  <label>
                    <span>Description</span>
                    <textarea {...tourBasicsForm.register('description', { required: true })} placeholder="Description" />
                  </label>
                  <div className="form-grid compact-grid">
                    <label>
                      <span>Difficulty</span>
                      <input {...tourBasicsForm.register('difficulty', { required: true })} placeholder="Difficulty" />
                    </label>
                    <label>
                      <span>Tags</span>
                      <input {...tourBasicsForm.register('tagsCsv')} placeholder="tag1, tag2" />
                    </label>
                  </div>
                  <button type="submit">Save details</button>
                </form>
                {tourError && <p className="message-error">{tourError}</p>}
                {tourSuccess && <p className="message-success">{tourSuccess}</p>}
              </details>

              <details className="control-section">
                <summary>
                    <span>Required for publish</span>
                    <strong>Travel times</strong>
                  </summary>
                  <form className="stacked-form compact-form" onSubmit={handleSubmitTravelTimes}>
                  <div className="form-grid compact-grid three-col-grid">
                    <label>
                      <span>Walking</span>
                      <input
                        {...travelTimesForm.register('walkingMinutes')}
                        type="number"
                        min="1"
                        placeholder="Minutes"
                      />
                    </label>
                    <label>
                      <span>Bicycle</span>
                      <input
                        {...travelTimesForm.register('bicycleMinutes')}
                        type="number"
                        min="1"
                        placeholder="Minutes"
                      />
                    </label>
                    <label>
                      <span>Car</span>
                      <input
                        {...travelTimesForm.register('carMinutes')}
                        type="number"
                        min="1"
                        placeholder="Minutes"
                      />
                    </label>
                  </div>
                  <p className="section-note">At least one transport time is required for publish.</p>
                  <button type="submit">Save travel times</button>
                </form>
                {travelTimesError && <p className="message-error">{travelTimesError}</p>}
                {travelTimesSuccess && <p className="message-success">{travelTimesSuccess}</p>}
              </details>

              <details className="control-section" open={editingKeyPointId !== null}>
                <summary>
                  <span>Map-assisted editing</span>
                  <strong>{editingKeyPointId === null ? 'Add key point' : 'Edit key point'}</strong>
                </summary>
                <form className="stacked-form compact-form" onSubmit={handleSubmitKeyPoint}>
                  <label>
                    <span>Name</span>
                    <input
                      {...keyPointForm.register('name', { required: true, minLength: 2 })}
                      placeholder="Key point name"
                    />
                  </label>
                  <label>
                    <span>Description</span>
                    <textarea
                      {...keyPointForm.register('description', { required: true, minLength: 5 })}
                      placeholder="Short description"
                    />
                  </label>
                  <div className="form-grid compact-grid">
                    <label>
                      <span>Latitude</span>
                      <input
                        {...keyPointForm.register('latitude', { required: true, valueAsNumber: true })}
                        placeholder="Latitude"
                        type="number"
                        step="any"
                      />
                    </label>
                    <label>
                      <span>Longitude</span>
                      <input
                        {...keyPointForm.register('longitude', { required: true, valueAsNumber: true })}
                        placeholder="Longitude"
                        type="number"
                        step="any"
                      />
                    </label>
                  </div>
                  <div className="form-grid compact-grid">
                    <label>
                      <span>Image URL</span>
                      <input {...keyPointForm.register('imageUrl')} placeholder="Optional image URL" />
                    </label>
                    <label>
                      <span>Order</span>
                      <input
                        {...keyPointForm.register('orderIndex', { required: true, valueAsNumber: true })}
                        placeholder="Order index"
                        type="number"
                        min="0"
                      />
                    </label>
                  </div>
                  <div className="inline-actions">
                    <button type="submit">{editingKeyPointId === null ? 'Add key point' : 'Save changes'}</button>
                    {editingKeyPointId !== null && (
                      <button type="button" className="button-secondary" onClick={resetKeyPointForm}>
                        Cancel
                      </button>
                    )}
                  </div>
                </form>
              </details>
            </section>
          )}
        </aside>
      </div>
    </section>
  )
}
