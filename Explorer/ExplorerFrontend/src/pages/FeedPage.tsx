import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getFeed } from '../api/followerApi'
import type { BlogResponse } from '../shared/types/blog'

export function FeedPage() {
  const [blogs, setBlogs] = useState<BlogResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const data = await getFeed()
        setBlogs(data)
      } catch {
        setError('Failed to load your feed.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [])

  if (loading) return <p>Loading feed...</p>

  return (
    <section className="feed-page">
      <div className="section-header">
        <div>
          <p className="section-eyebrow">Blogs from people you follow</p>
          <h1>My Feed</h1>
        </div>
        <Link to="/blogs" className="pill">
          Discover all blogs
        </Link>
      </div>
      {error && <p className="message-error">{error}</p>}
      {blogs.length === 0 ? (
        <div className="card">
          <p className="empty-state">
            Your feed is empty. <Link to="/explorers">Follow some explorers</Link> to see
            their blogs here.
          </p>
        </div>
      ) : (
        <ul className="blog-list">
          {blogs.map((blog) => (
            <li key={blog.id} className="card blog-card">
              <Link className="blog-card-title" to={`/blogs/${blog.id}`}>
                {blog.title}
              </Link>
              <p className="blog-card-meta">
                by <strong>{blog.authorUsername}</strong> ·{' '}
                {new Date(blog.createdAtUtc).toLocaleDateString()} · ❤ {blog.likesCount}
              </p>
              <p className="blog-card-excerpt">{blog.descriptionMarkdown}</p>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
