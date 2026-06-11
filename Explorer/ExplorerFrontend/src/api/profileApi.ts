import { apiHttpClient } from './httpClient'

export type ProfileResponse = {
  userId: number
  firstName: string
  lastName: string
  profileImageUrl: string | null
  biography: string | null
  motto: string | null
}

export type UpdateProfilePayload = {
  firstName: string
  lastName: string
  profileImageUrl?: string | null
  biography?: string | null
  motto?: string | null
}

export async function getMyProfile() {
  const response = await apiHttpClient.get<ProfileResponse>('/api/profiles/me')
  return response.data
}

export async function updateMyProfile(payload: UpdateProfilePayload) {
  const response = await apiHttpClient.put<ProfileResponse>('/api/profiles/me', payload)
  return response.data
}
