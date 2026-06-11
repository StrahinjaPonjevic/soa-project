import { apiHttpClient } from './httpClient'
import type { BlogResponse, CommentResponse } from '../shared/types/blog'

export async function getBlogs() {
  const response = await apiHttpClient.get<BlogResponse[]>('/api/blogs')
  return response.data
}

export async function getBlog(id: number) {
  const response = await apiHttpClient.get<BlogResponse>(`/api/blogs/${id}`)
  return response.data
}

export async function createBlog(payload: {
  title: string
  descriptionMarkdown: string
  imageUrls?: string[]
}) {
  const response = await apiHttpClient.post<BlogResponse>('/api/blogs', payload)
  return response.data
}

export async function addComment(blogId: number, text: string) {
  const response = await apiHttpClient.post<CommentResponse>(`/api/blogs/${blogId}/comments`, {
    text,
  })
  return response.data
}

export async function updateComment(blogId: number, commentId: number, text: string) {
  const response = await apiHttpClient.put<CommentResponse>(
    `/api/blogs/${blogId}/comments/${commentId}`,
    { text },
  )
  return response.data
}

export async function likeBlog(blogId: number) {
  await apiHttpClient.post(`/api/blogs/${blogId}/likes`)
}

export async function unlikeBlog(blogId: number) {
  await apiHttpClient.delete(`/api/blogs/${blogId}/likes`)
}
