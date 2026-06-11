import { apiHttpClient } from './httpClient'
import type { BlogResponse } from '../shared/types/blog'
import type { Recommendation } from '../shared/types/social'

export async function followUser(followingId: number) {
  await apiHttpClient.post('/api/followers/follow', { followingId })
}

export async function unfollowUser(followingId: number) {
  await apiHttpClient.delete('/api/followers/follow', { data: { followingId } })
}

export async function getFollowing(userId: number) {
  const response = await apiHttpClient.get<number[] | null>(`/api/followers/following/${userId}`)
  return response.data ?? []
}

export async function getFollowers(userId: number) {
  const response = await apiHttpClient.get<number[] | null>(`/api/followers/followers/${userId}`)
  return response.data ?? []
}

export async function isFollowing(followerId: number, followingId: number) {
  const response = await apiHttpClient.get<{ isFollowing: boolean }>('/api/followers/is-following', {
    params: { followerId, followingId },
  })
  return response.data.isFollowing
}

export async function getRecommendations(userId: number) {
  const response = await apiHttpClient.get<Recommendation[] | null>(
    `/api/followers/recommendations/${userId}`,
  )
  return response.data ?? []
}

export async function getFeed() {
  const response = await apiHttpClient.get<BlogResponse[] | null>('/api/followers/feed')
  return response.data ?? []
}
