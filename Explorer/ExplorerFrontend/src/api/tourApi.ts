import { tourHttpClient } from './httpClient'
import type { RoutePointResponse, TourResponse, TransportType, TourTravelTimeResponse } from '../shared/types/tour'
import type { TourReviewResponse } from '../shared/types/review'

function normalizeTour(raw: Partial<TourResponse> & { id: number }): TourResponse {
  return {
    id: raw.id,
    authorId: raw.authorId ?? 0,
    authorUsername: raw.authorUsername ?? '',
    name: raw.name ?? '',
    description: raw.description ?? '',
    difficulty: raw.difficulty ?? '',
    tags: raw.tags ?? [],
    status: raw.status ?? 'Draft',
    price: raw.price ?? 0,
    lengthKm: raw.lengthKm ?? 0,
    createdAtUtc: raw.createdAtUtc ?? new Date(0).toISOString(),
    publishedAtUtc: raw.publishedAtUtc ?? null,
    archivedAtUtc: raw.archivedAtUtc ?? null,
    keyPoints: raw.keyPoints ?? [],
    travelTimes: raw.travelTimes ?? [],
  }
}

export async function getTours() {
  const response = await tourHttpClient.get<Array<Partial<TourResponse> & { id: number }>>('/api/tours')
  return response.data.map(normalizeTour)
}

export async function getMyTours() {
  const response = await tourHttpClient.get<Array<Partial<TourResponse> & { id: number }>>('/api/tours/me')
  return response.data.map(normalizeTour)
}

export async function createTour(payload: {
  name: string
  description: string
  difficulty: string
  tags: string[]
}) {
  const response = await tourHttpClient.post<Partial<TourResponse> & { id: number }>('/api/tours', payload)
  return normalizeTour(response.data)
}

export async function getTourById(tourId: number) {
  const response = await tourHttpClient.get<Partial<TourResponse> & { id: number }>(`/api/tours/${tourId}`)
  return normalizeTour(response.data)
}

export async function updateTour(
  tourId: number,
  payload: {
    name: string
    description: string
    difficulty: string
    tags: string[]
  },
) {
  const response = await tourHttpClient.put<Partial<TourResponse> & { id: number }>(`/api/tours/${tourId}`, payload)
  return normalizeTour(response.data)
}

export async function addKeyPoint(
  tourId: number,
  payload: {
    name: string
    description: string
    latitude: number
    longitude: number
    imageUrl?: string | null
    orderIndex: number
  },
) {
  const response = await tourHttpClient.post(`/api/tours/${tourId}/keypoints`, payload)
  return response.data
}

export async function updateKeyPoint(
  tourId: number,
  keyPointId: number,
  payload: {
    name: string
    description: string
    latitude: number
    longitude: number
    imageUrl?: string | null
    orderIndex: number
  },
) {
  const response = await tourHttpClient.put(`/api/tours/${tourId}/keypoints/${keyPointId}`, payload)
  return response.data
}

export async function deleteKeyPoint(tourId: number, keyPointId: number) {
  await tourHttpClient.delete(`/api/tours/${tourId}/keypoints/${keyPointId}`)
}

export async function replaceTravelTimes(
  tourId: number,
  payload: {
    travelTimes: Array<{
      transportType: TransportType
      durationMinutes: number
    }>
  },
) {
  const response = await tourHttpClient.put<TourTravelTimeResponse[]>(`/api/tours/${tourId}/travel-times`, payload)
  return response.data
}

export async function publishTour(tourId: number) {
  const response = await tourHttpClient.post<Partial<TourResponse> & { id: number }>(`/api/tours/${tourId}/publish`)
  return normalizeTour(response.data)
}

export async function archiveTour(tourId: number) {
  const response = await tourHttpClient.post<Partial<TourResponse> & { id: number }>(`/api/tours/${tourId}/archive`)
  return normalizeTour(response.data)
}

export async function reactivateTour(tourId: number) {
  const response = await tourHttpClient.post<Partial<TourResponse> & { id: number }>(`/api/tours/${tourId}/reactivate`)
  return normalizeTour(response.data)
}

export async function getTourRoutePreview(tourId: number) {
  const response = await tourHttpClient.get<{ points?: RoutePointResponse[] }>(`/api/tours/${tourId}/route-preview`)
  return response.data.points ?? []
}

export async function getTourReviews(tourId: number) {
  const response = await tourHttpClient.get<TourReviewResponse[]>(`/api/tours/${tourId}/reviews`)
  return response.data
}

export async function addTourReview(
  tourId: number,
  payload: {
    rating: number
    comment: string
    visitedAtUtc: string
    imageUrls: string[]
  },
) {
  const response = await tourHttpClient.post<TourReviewResponse>(`/api/tours/${tourId}/reviews`, payload)
  return response.data
}

export type TouristLocationResponse = {
  latitude: number
  longitude: number
  updatedAtUtc: string
}

export async function getMyTouristLocation() {
  const response = await tourHttpClient.get<TouristLocationResponse>('/api/tours/me/location')
  return response.data
}

export async function setMyTouristLocation(payload: { latitude: number; longitude: number }) {
  const response = await tourHttpClient.put<TouristLocationResponse>('/api/tours/me/location', payload)
  return response.data
}
