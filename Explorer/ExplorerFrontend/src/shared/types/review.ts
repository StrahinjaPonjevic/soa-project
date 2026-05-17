export type TourReviewResponse = {
  id: string
  tourId: number
  touristId: number
  touristUsername: string
  rating: number
  comment: string
  visitedAtUtc: string
  createdAtUtc: string
  imageUrls: string[]
}
