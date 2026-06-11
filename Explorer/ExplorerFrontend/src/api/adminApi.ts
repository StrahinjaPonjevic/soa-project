import { apiHttpClient } from './httpClient'

export type UserAccount = {
  id: number
  username: string
  email: string
  role: string
  isBlocked: boolean
}

export async function getAllUsers() {
  const response = await apiHttpClient.get<UserAccount[]>('/api/auth/users')
  return response.data
}

export async function blockUser(id: number) {
  const response = await apiHttpClient.patch<UserAccount>(`/api/auth/users/${id}/block`)
  return response.data
}

export async function unblockUser(id: number) {
  const response = await apiHttpClient.patch<UserAccount>(`/api/auth/users/${id}/unblock`)
  return response.data
}
