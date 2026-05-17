import { tourHttpClient } from './httpClient'
import type { TourResponse } from '../shared/types/tour'
import type { TourReviewResponse } from '../shared/types/review'

export async function getTours() {
  const response = await tourHttpClient.get<TourResponse[]>('/api/tours')
  return response.data
}

export async function getMyTours() {
  const response = await tourHttpClient.get<TourResponse[]>('/api/tours/me')
  return response.data
}

export async function createTour(payload: {
  name: string
  description: string
  difficulty: string
  tags: string[]
}) {
  const response = await tourHttpClient.post<TourResponse>('/api/tours', payload)
  return response.data
}

export async function getTourById(tourId: number) {
  const response = await tourHttpClient.get<TourResponse>(`/api/tours/${tourId}`)
  return response.data
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
