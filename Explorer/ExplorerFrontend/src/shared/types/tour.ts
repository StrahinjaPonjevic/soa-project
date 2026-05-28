export type KeyPointResponse = {
  id: number
  tourId: number
  name: string
  description: string
  latitude: number
  longitude: number
  imageUrl?: string | null
  orderIndex: number
}

export type TransportType = 'Walking' | 'Bicycle' | 'Car'

export type TourTravelTimeResponse = {
  transportType: TransportType
  durationMinutes: number
}

export type RoutePointResponse = {
  latitude: number
  longitude: number
}

export type TourResponse = {
  id: number
  authorId: number
  authorUsername: string
  name: string
  description: string
  difficulty: string
  tags: string[]
  status: string
  price: number
  lengthKm: number
  createdAtUtc: string
  publishedAtUtc?: string | null
  archivedAtUtc?: string | null
  keyPoints: KeyPointResponse[]
  travelTimes: TourTravelTimeResponse[]
}
