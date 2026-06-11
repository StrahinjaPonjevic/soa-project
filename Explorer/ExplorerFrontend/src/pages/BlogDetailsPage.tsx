import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { addComment, getBlog, likeBlog, unlikeBlog, updateComment } from '../api/blogApi'
import { followUser, isFollowing, unfollowUser } from '../api/followerApi'
import type { BlogResponse } from '../shared/types/blog'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

export function BlogDetailsPage() {
  const { id } = useParams()
  const blogId = Number(id)
  const { token } = useAuth()
  const myId = parseAuthUser(token).userId

  const [blog, setBlog] = useState<BlogResponse | null>(null)
  const [followsAuthor, setFollowsAuthor] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [commentText, setCommentText] = useState('')
  const [commentError, setCommentError] = useState<string | null>(null)
  const [submittingComment, setSubmittingComment] = useState(false)

  const [editingCommentId, setEditingCommentId] = useState<number | null>(null)
  const [editingText, setEditingText] = useState('')

  const [followBusy, setFollowBusy] = useState(false)
  const [likeBusy, setLikeBusy] = useState(false)

  const isAuthor = blog !== null && blog.authorId === myId
  const canRead = isAuthor || followsAuthor

  const loadBlog = useCallback(async () => {
    const data = await getBlog(blogId)
    setBlog(data)
    if (myId && data.authorId !== myId) {
      setFollowsAuthor(await isFollowing(myId, data.authorId))
    }
    return data
  }, [blogId, myId])

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        await loadBlog()
      } catch {
        setError('Failed to load blog.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [loadBlog])

  const handleToggleFollow = async () => {
    if (!blog) return
    setFollowBusy(true)
    setError(null)
    try {
      if (followsAuthor) {
        await unfollowUser(blog.authorId)
        setFollowsAuthor(false)
      } else {
        await followUser(blog.authorId)
        setFollowsAuthor(true)
      }
    } catch {
      setError('Failed to update follow status.')
    } finally {
      setFollowBusy(false)
    }
  }

  const handleLike = async (like: boolean) => {
    setLikeBusy(true)
    try {
      if (like) {
        await likeBlog(blogId)
      } else {
        await unlikeBlog(blogId)
      }
      await loadBlog()
    } catch {
      setError('Failed to update like.')
    } finally {
      setLikeBusy(false)
    }
  }

  const handleAddComment = async (event: FormEvent) => {
    event.preventDefault()
    setCommentError(null)
    setSubmittingComment(true)
    try {
      await addComment(blogId, commentText.trim())
      setCommentText('')
      await loadBlog()
    } catch (err: any) {
      if (err?.response?.status === 403) {
        setCommentError('You can only comment on blogs of users you follow.')
      } else {
        setCommentError('Failed to add comment.')
      }
    } finally {
      setSubmittingComment(false)
    }
  }

  const handleSaveEdit = async (commentId: number) => {
    setCommentError(null)
    try {
      await updateComment(blogId, commentId, editingText.trim())
      setEditingCommentId(null)
      await loadBlog()
    } catch {
      setCommentError('Failed to update comment.')
    }
  }

  if (loading) return <p>Loading blog...</p>
  if (!blog) {
    return (
      <div className="card">
        <p className="empty-state">Blog not found.</p>
        {error && <p className="message-error">{error}</p>}
      </div>
    )
  }

  return (
    <section className="blog-details-page">
      <div className="card">
        <div className="section-header">
          <div>
            <p className="section-eyebrow">
              by {blog.authorUsername} · {new Date(blog.createdAtUtc).toLocaleString()}
            </p>
            <h1>{blog.title}</h1>
          </div>
          {!isAuthor && (
            <button
              type="button"
              className={followsAuthor ? 'button-secondary' : undefined}
              onClick={() => void handleToggleFollow()}
              disabled={followBusy}
            >
              {followBusy ? '...' : followsAuthor ? 'Unfollow' : 'Follow'}
            </button>
          )}
        </div>

        {error && <p className="message-error">{error}</p>}

        {canRead ? (
          <>
            <p className="blog-content">{blog.descriptionMarkdown}</p>
            {blog.imageUrls && blog.imageUrls.length > 0 && (
              <div className="blog-images">
                {blog.imageUrls.map((url) => (
                  <img key={url} src={url} alt="" />
                ))}
              </div>
            )}
            <div className="inline-actions blog-like-actions">
              <span className="pill">❤ {blog.likesCount}</span>
              <button
                type="button"
                onClick={() => void handleLike(true)}
                disabled={likeBusy}
              >
                Like
              </button>
              <button
                type="button"
                className="button-secondary"
                onClick={() => void handleLike(false)}
                disabled={likeBusy}
              >
                Unlike
              </button>
            </div>
          </>
        ) : (
          <div className="blog-locked-note">
            <p>
              🔒 You can read blogs only from explorers you follow. Follow{' '}
              <strong>{blog.authorUsername}</strong> to unlock this post.
            </p>
          </div>
        )}
      </div>

      {canRead && (
        <div className="card">
          <div className="section-header">
            <h2>Comments ({blog.comments?.length ?? 0})</h2>
          </div>

          {(blog.comments ?? []).length === 0 ? (
            <p className="empty-state">No comments yet.</p>
          ) : (
            <ul className="comment-list">
              {(blog.comments ?? []).map((comment) => (
                <li key={comment.id} className="comment-card">
                  <div className="comment-head">
                    <strong>{comment.authorUsername}</strong>
                    <span className="comment-date">
                      {new Date(comment.createdAtUtc).toLocaleString()}
                      {comment.updatedAtUtc !== comment.createdAtUtc && ' (edited)'}
                    </span>
                  </div>
                  {editingCommentId === comment.id ? (
                    <div className="stacked-form">
                      <textarea
                        value={editingText}
                        onChange={(e) => setEditingText(e.target.value)}
                        rows={3}
                      />
                      <div className="inline-actions">
                        <button
                          type="button"
                          onClick={() => void handleSaveEdit(comment.id)}
                          disabled={editingText.trim().length === 0}
                        >
                          Save
                        </button>
                        <button
                          type="button"
                          className="button-secondary"
                          onClick={() => setEditingCommentId(null)}
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  ) : (
                    <>
                      <p className="comment-text">{comment.text}</p>
                      {comment.authorId === myId && (
                        <button
                          type="button"
                          className="button-secondary"
                          onClick={() => {
                            setEditingCommentId(comment.id)
                            setEditingText(comment.text)
                          }}
                        >
                          Edit
                        </button>
                      )}
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}

          <form className="stacked-form comment-form" onSubmit={(e) => void handleAddComment(e)}>
            <label>
              <span>Add a comment</span>
              <textarea
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                rows={3}
                maxLength={2000}
                required
                placeholder="Write your comment..."
              />
            </label>
            {commentError && <p className="message-error">{commentError}</p>}
            <div className="inline-actions">
              <button type="submit" disabled={submittingComment || commentText.trim().length === 0}>
                {submittingComment ? 'Posting...' : 'Post comment'}
              </button>
            </div>
          </form>
        </div>
      )}

      <p>
        <Link to="/blogs">← Back to all blogs</Link>
      </p>
    </section>
  )
}
