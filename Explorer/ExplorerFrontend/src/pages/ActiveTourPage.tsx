import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MapContainer, Marker, Popup, TileLayer, useMapEvents } from 'react-leaflet'
import type { LeafletMouseEvent } from 'leaflet'
import L from 'leaflet'
import {
  abandonTour,
  checkNearbyKeyPoint,
  completeTour,
  getTourExecution,
  getExecutionKeyPoints,
  getActiveTourExecution,
  getMyTouristLocation,
  setMyTouristLocation,
} from '../api/tourApi'
import type { TourExecutionResponse, CheckNearbyResponse } from '../api/tourApi'
import type { KeyPointResponse } from '../shared/types/tour'

const CHECK_INTERVAL_MS = 10_000

const LeafletMapContainer = MapContainer as any
const LeafletTileLayer = TileLayer as any

function MapClickHandler({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (e: LeafletMouseEvent) => onPick(e.latlng.lat, e.latlng.lng),
  })
  return null
}

// Green icon for completed key points, orange for pending
const makeIcon = (color: 'orange' | 'green') =>
  new L.Icon({
    iconUrl: `https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-${color}.png`,
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
    shadowSize: [41, 41],
  })

const greenIcon = makeIcon('green')
const orangeIcon = makeIcon('orange')
const blueIcon = new L.Icon.Default()

export function ActiveTourPage() {
  const { executionId } = useParams<{ executionId: string }>()
  const navigate = useNavigate()

  const [execution, setExecution] = useState<TourExecutionResponse | null>(null)
  const [tourName, setTourName] = useState<string>('')
  const [keyPoints, setKeyPoints] = useState<KeyPointResponse[]>([])
  const [myPosition, setMyPosition] = useState<{ lat: number; lng: number } | null>(null)
  const [lastCheck, setLastCheck] = useState<CheckNearbyResponse | null>(null)
  const [notification, setNotification] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [ending, setEnding] = useState(false)

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const execId = Number(executionId)

  // Initial load
  useEffect(() => {
    const load = async () => {
      try {
        let exec = await getTourExecution(execId).catch(async (err) => {
          // If this specific execution isn't found, try the tourist's active one
          if (err?.response?.status === 404) {
            const active = await getActiveTourExecution()
            navigate(`/tours/executions/${active.id}`, { replace: true })
            return active
          }
          throw err
        })

        const loc = await getMyTouristLocation().catch(() => null)
        setExecution(exec)
        if (loc) setMyPosition({ lat: loc.latitude, lng: loc.longitude })

        if (exec.status === 'Active') {
          const kps = await getExecutionKeyPoints(exec.id)
          setKeyPoints(kps)
        }
      } catch {
        setError('Failed to load tour execution.')
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [execId])

  // 10-second proximity check
  const runCheck = useCallback(async () => {
    try {
      const result = await checkNearbyKeyPoint(execId)
      setExecution(result.execution)

      // Refresh position from simulator
      const loc = await getMyTouristLocation().catch(() => null)
      if (loc) setMyPosition({ lat: loc.latitude, lng: loc.longitude })

      setLastCheck(result)
      if (result.isNewlyCompleted && result.keyPointName) {
        setNotification(`✓ Reached: ${result.keyPointName}`)
        setTimeout(() => setNotification(null), 5000)
      }
    } catch {
      // Silent — next tick will retry
    }
  }, [execId])

  useEffect(() => {
    if (!execution || execution.status !== 'Active') return

    intervalRef.current = setInterval(() => void runCheck(), CHECK_INTERVAL_MS)
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [execution?.status, runCheck])

  const handleMapClick = useCallback(async (lat: number, lng: number) => {
    try {
      await setMyTouristLocation({ latitude: lat, longitude: lng })
      setMyPosition({ lat, lng })
      // Immediately check proximity after moving
      const result = await checkNearbyKeyPoint(execId)
      setExecution(result.execution)
      setLastCheck(result)
      if (result.isNewlyCompleted && result.keyPointName) {
        setNotification(`✓ Reached: ${result.keyPointName}`)
        setTimeout(() => setNotification(null), 5000)
      }
    } catch {
      // silent
    }
  }, [execId])

  const handleComplete = async () => {
    if (!window.confirm('Mark this tour as completed?')) return
    setEnding(true)
    try {
      await completeTour(execId)
      navigate('/tours', { state: { message: 'Tour completed!' } })
    } catch {
      setError('Failed to complete tour.')
      setEnding(false)
    }
  }

  const handleAbandon = async () => {
    if (!window.confirm('Abandon this tour?')) return
    setEnding(true)
    try {
      await abandonTour(execId)
      navigate('/tours', { state: { message: 'Tour abandoned.' } })
    } catch {
      setError('Failed to abandon tour.')
      setEnding(false)
    }
  }

  if (loading) return <p>Loading active tour...</p>
  if (error) return <p style={{ color: 'var(--danger)' }}>{error}</p>
  if (!execution) return <p>Execution not found.</p>

  if (execution.status !== 'Active') {
    return (
      <section className="page-container">
        <h1>Tour Execution</h1>
        <p>
          Status: <strong>{execution.status}</strong>
        </p>
        <button type="button" onClick={() => navigate('/tours')}>
          Back to Tours
        </button>
      </section>
    )
  }

  const completedIds = new Set(execution.completedKeyPoints.map((ckp) => ckp.keyPointId))
  const totalKp = keyPoints.length
  const doneKp = completedIds.size

  const mapCenter: [number, number] = myPosition
    ? [myPosition.lat, myPosition.lng]
    : keyPoints.length > 0
      ? [keyPoints[0].latitude, keyPoints[0].longitude]
      : [execution.initialLatitude, execution.initialLongitude]

  return (
    <section>
      <h1>Active Tour</h1>

      {notification && (
        <div className="tour-notification">
          {notification}
        </div>
      )}

      <div className="active-tour-meta">
        <span>
          Key points: <strong>{doneKp} / {totalKp}</strong> completed
        </span>
        <span>
          Started: <strong>{new Date(execution.startedAtUtc).toLocaleTimeString()}</strong>
        </span>
        <span>
          Last check: <strong>{new Date(execution.lastActivityAtUtc).toLocaleTimeString()}</strong>
        </span>
      </div>

      {lastCheck && !lastCheck.isNearKeyPoint && (
        <p className="nearby-status nearby-none">Not near any key point yet. Keep walking!</p>
      )}

      <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: '0 0 0.5rem' }}>
        📍 Click anywhere on the map to move your position — proximity is checked instantly.
      </p>

      <div className="map-container" style={{ marginBottom: '1rem' }}>
        <LeafletMapContainer center={mapCenter} zoom={14} scrollWheelZoom className="map-view">
          <LeafletTileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <MapClickHandler onPick={handleMapClick} />

          {/* Tourist's current position */}
          {myPosition && (
            <Marker position={[myPosition.lat, myPosition.lng]} icon={blueIcon}>
              <Popup>You are here</Popup>
            </Marker>
          )}

          {/* Key points — green if completed, orange if pending */}
          {keyPoints.map((kp) => (
            <Marker
              key={kp.id}
              position={[kp.latitude, kp.longitude]}
              icon={completedIds.has(kp.id) ? greenIcon : orangeIcon}
            >
              <Popup>
                <strong>{kp.name}</strong>
                <br />
                {kp.description}
                <br />
                {completedIds.has(kp.id) ? (
                  <span style={{ color: 'green' }}>✓ Completed</span>
                ) : (
                  <span style={{ color: '#ea580c' }}>Pending</span>
                )}
              </Popup>
            </Marker>
          ))}
        </LeafletMapContainer>
      </div>

      <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1rem' }}>
        <button type="button" onClick={() => void runCheck()} style={{ background: 'var(--accent)' }}>
          Check Nearby Now
        </button>
        <button
          type="button"
          onClick={handleComplete}
          disabled={ending}
          style={{ background: 'var(--success)' }}
        >
          Complete Tour
        </button>
        <button
          type="button"
          onClick={handleAbandon}
          disabled={ending}
          style={{ background: 'var(--danger)' }}
        >
          Abandon Tour
        </button>
      </div>

      {keyPoints.length > 0 && (
        <div className="keypoints-list">
          <h2>Key Points</h2>
          <ul>
            {keyPoints.map((kp) => {
              const done = completedIds.has(kp.id)
              const ckp = execution.completedKeyPoints.find((c) => c.keyPointId === kp.id)
              return (
                <li key={kp.id} className={`keypoint-item ${done ? 'keypoint-done' : ''}`}>
                  <span className="keypoint-marker">{done ? '✓' : '○'}</span>
                  <span className="keypoint-name">{kp.name}</span>
                  {done && ckp && (
                    <span className="keypoint-time">
                      {new Date(ckp.completedAtUtc).toLocaleTimeString()}
                    </span>
                  )}
                </li>
              )
            })}
          </ul>
        </div>
      )}

      {error && <p style={{ color: 'var(--danger)' }}>{error}</p>}

      <p style={{ fontSize: '0.8rem', color: '#888', marginTop: '1rem' }}>
        Proximity check runs automatically every 10 seconds. Click the map or use "Check Nearby Now" to trigger manually.
      </p>
    </section>
  )
}
