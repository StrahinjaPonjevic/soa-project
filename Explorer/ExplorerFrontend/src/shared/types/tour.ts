export type KeyPointResponse = {
  id: number
  tourId: number
  name: string
  description: string
  latitude: number
  longitude: number
  imageUrl?: string | null
  orderIndex: number
}

export type TourResponse = {
  id: number
  authorId: number
  authorUsername: string
  name: string
  description: string
  difficulty: string
  tags: string[]
  status: string
  price: number
  createdAtUtc: string
  keyPoints: KeyPointResponse[]
}
