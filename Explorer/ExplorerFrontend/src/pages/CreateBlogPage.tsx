import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { createBlog } from '../api/blogApi'

export function CreateBlogPage() {
  const navigate = useNavigate()

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [imageUrls, setImageUrls] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const urls = imageUrls
        .split('\n')
        .map((url) => url.trim())
        .filter((url) => url.length > 0)

      const created = await createBlog({
        title: title.trim(),
        descriptionMarkdown: description.trim(),
        imageUrls: urls.length > 0 ? urls : undefined,
      })
      navigate(`/blogs/${created.id}`)
    } catch (err: any) {
      const msg =
        typeof err?.response?.data === 'string'
          ? err.response.data
          : 'Failed to create blog. Title must be 3-120 characters.'
      setError(msg)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="create-blog-page">
      <h1>Write a Blog</h1>
      <div className="card">
        <form className="stacked-form" onSubmit={(e) => void handleSubmit(e)}>
          <label>
            <span>Title</span>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              minLength={3}
              maxLength={120}
              required
              placeholder="My adventure in the mountains"
            />
          </label>
          <label>
            <span>Content (markdown)</span>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
              rows={10}
              placeholder="Share your experience..."
            />
          </label>
          <label>
            <span>Image URLs (optional, one per line)</span>
            <textarea
              value={imageUrls}
              onChange={(e) => setImageUrls(e.target.value)}
              rows={3}
              placeholder="https://example.com/photo.jpg"
            />
          </label>
          {error && <p className="message-error">{error}</p>}
          <div className="inline-actions">
            <button type="submit" disabled={submitting}>
              {submitting ? 'Publishing...' : 'Publish'}
            </button>
            <button
              type="button"
              className="button-secondary"
              onClick={() => navigate('/blogs')}
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </section>
  )
}
