import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getBlogs } from '../api/blogApi'
import { followUser, getFollowing } from '../api/followerApi'
import type { BlogResponse } from '../shared/types/blog'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

export function BlogsPage() {
  const { token } = useAuth()
  const myId = parseAuthUser(token).userId

  const [blogs, setBlogs] = useState<BlogResponse[]>([])
  const [followingIds, setFollowingIds] = useState<number[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busyAuthorId, setBusyAuthorId] = useState<number | null>(null)

  const followingSet = useMemo(() => new Set(followingIds), [followingIds])

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const [allBlogs, following] = await Promise.all([
          getBlogs(),
          myId ? getFollowing(myId) : Promise.resolve([]),
        ])
        setBlogs(allBlogs)
        setFollowingIds(following)
      } catch {
        setError('Failed to load blogs.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [myId])

  const handleFollow = async (authorId: number) => {
    setBusyAuthorId(authorId)
    setError(null)
    try {
      await followUser(authorId)
      setFollowingIds((prev) => [...prev, authorId])
    } catch {
      setError('Failed to follow author.')
    } finally {
      setBusyAuthorId(null)
    }
  }

  if (loading) return <p>Loading blogs...</p>

  return (
    <section className="blogs-page">
      <div className="section-header">
        <div>
          <p className="section-eyebrow">Discover</p>
          <h1>All Blogs</h1>
          <p className="section-note">
            You can read blogs only from explorers you follow. Follow an author to unlock
            their posts.
          </p>
        </div>
        <Link to="/blogs/new">
          <button type="button">Write a blog</button>
        </Link>
      </div>
      {error && <p className="message-error">{error}</p>}
      {blogs.length === 0 ? (
        <div className="card">
          <p className="empty-state">No blogs published yet. Be the first to write one!</p>
        </div>
      ) : (
        <ul className="blog-list">
          {blogs.map((blog) => {
            const canRead = blog.authorId === myId || followingSet.has(blog.authorId)
            return (
              <li key={blog.id} className={`card blog-card${canRead ? '' : ' blog-card-locked'}`}>
                {canRead ? (
                  <Link className="blog-card-title" to={`/blogs/${blog.id}`}>
                    {blog.title}
                  </Link>
                ) : (
                  <span className="blog-card-title">🔒 {blog.title}</span>
                )}
                <p className="blog-card-meta">
                  by <strong>{blog.authorUsername}</strong> ·{' '}
                  {new Date(blog.createdAtUtc).toLocaleDateString()} · ❤ {blog.likesCount} ·{' '}
                  💬 {blog.comments?.length ?? 0}
                </p>
                {canRead ? (
                  <p className="blog-card-excerpt">{blog.descriptionMarkdown}</p>
                ) : (
                  <div className="blog-locked-note">
                    <p>Follow {blog.authorUsername} to read this blog.</p>
                    <button
                      type="button"
                      onClick={() => void handleFollow(blog.authorId)}
                      disabled={busyAuthorId === blog.authorId}
                    >
                      {busyAuthorId === blog.authorId ? 'Following...' : 'Follow'}
                    </button>
                  </div>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
