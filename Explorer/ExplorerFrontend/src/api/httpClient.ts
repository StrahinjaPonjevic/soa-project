import axios from 'axios'

const gatewayBaseURL = import.meta.env.VITE_API_GATEWAY_BASE_URL ?? 'http://localhost:5000'

export const authHttpClient = axios.create({
  baseURL: gatewayBaseURL,
})

export const tourHttpClient = axios.create({
  baseURL: gatewayBaseURL,
})

tourHttpClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('explorer_auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})
