export type CommentResponse = {
  id: number
  blogId: number
  authorId: number
  authorUsername: string
  text: string
  createdAtUtc: string
  updatedAtUtc: string
}

export type BlogResponse = {
  id: number
  title: string
  descriptionMarkdown: string
  authorId: number
  authorUsername: string
  createdAtUtc: string
  imageUrls: string[] | null
  likesCount: number
  // Follower service feed ne vraća komentare, blog servis vraća
  comments?: CommentResponse[]
}
