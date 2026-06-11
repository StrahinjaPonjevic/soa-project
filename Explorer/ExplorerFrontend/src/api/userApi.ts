import { apiHttpClient } from './httpClient'
import type { PublicUser } from '../shared/types/social'

export async function getPublicUsers() {
  const response = await apiHttpClient.get<PublicUser[]>('/api/auth/users/public')
  return response.data
}
