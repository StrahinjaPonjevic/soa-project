import { useEffect, useMemo, useState } from 'react'
import { MapContainer, Marker, TileLayer, useMapEvents } from 'react-leaflet'
import type { LeafletMouseEvent } from 'leaflet'
import { getMyTouristLocation, setMyTouristLocation } from '../api/tourApi'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

const FALLBACK_POSITION: [number, number] = [44.7866, 20.4489]

type Position = {
  latitude: number
  longitude: number
}

const LeafletMapContainer = MapContainer as any
const LeafletTileLayer = TileLayer as any

function MapClickHandler({ onPick }: { onPick: (position: Position) => void }) {
  useMapEvents({
    click: (event: LeafletMouseEvent) => {
      onPick({
        latitude: event.latlng.lat,
        longitude: event.latlng.lng,
      })
    },
  })
  return null
}

export function SimulatorPage() {
  const { token } = useAuth()
  const authUser = parseAuthUser(token)
  const [currentLocation, setCurrentLocation] = useState<Position | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    if (authUser.role !== 'Tourist') {
      setLoading(false)
      return
    }

    const load = async () => {
      try {
        setError(null)
        const location = await getMyTouristLocation()
        setCurrentLocation({
          latitude: location.latitude,
          longitude: location.longitude,
        })
      } catch {
        setCurrentLocation(null)
      } finally {
        setLoading(false)
      }
    }

    void load()
  }, [authUser.role])

  const center = useMemo<[number, number]>(() => {
    if (currentLocation) {
      return [currentLocation.latitude, currentLocation.longitude]
    }
    return FALLBACK_POSITION
  }, [currentLocation])

  const onPickLocation = async (position: Position) => {
    try {
      setError(null)
      setSuccess(null)
      const saved = await setMyTouristLocation(position)
      setCurrentLocation({
        latitude: saved.latitude,
        longitude: saved.longitude,
      })
      setSuccess('Current location updated.')
    } catch {
      setError('Failed to update current location.')
    }
  }

  if (loading) return <p>Loading simulator...</p>
  if (authUser.role !== 'Tourist') return <p>Simulator is available only for tourist accounts.</p>

  return (
    <section>
      <h1>Position Simulator</h1>
      <p>Click on map to set your current location.</p>

      <div className="map-container">
        <LeafletMapContainer center={center} zoom={13} scrollWheelZoom className="map-view">
          <LeafletTileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <MapClickHandler onPick={onPickLocation} />
          {currentLocation && <Marker position={[currentLocation.latitude, currentLocation.longitude]} />}
        </LeafletMapContainer>
      </div>

      {currentLocation ? (
        <p>
          Current location: {currentLocation.latitude.toFixed(6)}, {currentLocation.longitude.toFixed(6)}
        </p>
      ) : (
        <p>Current location is not set yet.</p>
      )}
      {error && <p>{error}</p>}
      {success && <p>{success}</p>}
    </section>
  )
}
