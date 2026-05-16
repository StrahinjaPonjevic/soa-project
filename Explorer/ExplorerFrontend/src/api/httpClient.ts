import axios from 'axios'

const authBaseURL = import.meta.env.VITE_AUTH_API_BASE_URL ?? 'http://localhost:5001'
const tourBaseURL = import.meta.env.VITE_TOUR_API_BASE_URL ?? 'http://localhost:5005'

export const authHttpClient = axios.create({
  baseURL: authBaseURL,
})

export const tourHttpClient = axios.create({
  baseURL: tourBaseURL,
})

tourHttpClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('explorer_auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})
