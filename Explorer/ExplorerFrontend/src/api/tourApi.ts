import { tourHttpClient } from './httpClient'
import type { TourResponse } from '../shared/types/tour'

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
