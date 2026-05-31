import { tourHttpClient } from './httpClient'

export type CartItem = {
  tourId: number
  tourName: string
  price: number
}

export type CartResponse = {
  touristId: number
  totalPrice: number
  items: CartItem[]
}

export type PurchaseToken = {
  tourId: number
  token: string
  purchasedAt: string
}

export async function getCart() {
  const response = await tourHttpClient.get<CartResponse>('/api/purchases/cart')
  return response.data
}

export async function addToCart(tourId: number, tourName: string, tourPrice: number, tourStatus: string) {
  const response = await tourHttpClient.post<CartResponse>('/api/purchases/cart/items', {
    tourId,
    tourName,
    tourPrice,
    tourStatus,
  })
  return response.data
}

export async function removeFromCart(tourId: number) {
  const response = await tourHttpClient.delete<CartResponse>(`/api/purchases/cart/items/${tourId}`)
  return response.data
}

export async function checkoutCart() {
  const response = await tourHttpClient.post<{ tokens: PurchaseToken[] }>('/api/purchases/cart/checkout')
  return response.data.tokens
}

export async function getPurchaseTokens() {
  const response = await tourHttpClient.get<{ tokens: PurchaseToken[] }>('/api/purchases/tokens')
  return response.data.tokens
}

export async function hasPurchaseToken(tourId: number) {
  const response = await tourHttpClient.get<{ tourId: number; purchased: boolean }>(
    `/api/purchases/tokens/${tourId}/exists`,
  )
  return response.data.purchased
}
