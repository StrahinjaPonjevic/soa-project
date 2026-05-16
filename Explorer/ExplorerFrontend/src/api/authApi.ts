import { authHttpClient } from './httpClient'

type LoginResponse = {
  token: string
}

export async function login(username: string, password: string) {
  const response = await authHttpClient.post<LoginResponse>('/api/auth/login', {
    username,
    password,
  })

  return response.data
}

export async function register(payload: {
  username: string
  email: string
  password: string
  role: 'Guide' | 'Tourist'
}) {
  await authHttpClient.post('/api/auth/register', payload)
}
