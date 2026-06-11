import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  followUser,
  getFollowers,
  getFollowing,
  getRecommendations,
  unfollowUser,
} from '../api/followerApi'
import { getPublicUsers } from '../api/userApi'
import type { PublicUser, Recommendation } from '../shared/types/social'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

export function ExplorersPage() {
  const { token } = useAuth()
  const myId = parseAuthUser(token).userId

  const [users, setUsers] = useState<PublicUser[]>([])
  const [followingIds, setFollowingIds] = useState<number[]>([])
  const [followerIds, setFollowerIds] = useState<number[]>([])
  const [recommendations, setRecommendations] = useState<Recommendation[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)

  const usernameOf = useMemo(() => {
    const map = new Map(users.map((u) => [u.id, u.username]))
    return (id: number) => map.get(id) ?? `User #${id}`
  }, [users])

  const followingSet = useMemo(() => new Set(followingIds), [followingIds])

  const refreshSocialGraph = useCallback(async () => {
    if (!myId) return
    const [following, followers, recs] = await Promise.all([
      getFollowing(myId),
      getFollowers(myId),
      getRecommendations(myId),
    ])
    setFollowingIds(following)
    setFollowerIds(followers)
    setRecommendations(recs)
  }, [myId])

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const [allUsers] = await Promise.all([getPublicUsers(), refreshSocialGraph()])
        setUsers(allUsers)
      } catch {
        setError('Failed to load explorers.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [refreshSocialGraph])

  const handleFollow = async (userId: number) => {
    setBusyId(userId)
    setError(null)
    try {
      await followUser(userId)
      await refreshSocialGraph()
    } catch {
      setError('Failed to follow user.')
    } finally {
      setBusyId(null)
    }
  }

  const handleUnfollow = async (userId: number) => {
    setBusyId(userId)
    setError(null)
    try {
      await unfollowUser(userId)
      await refreshSocialGraph()
    } catch {
      setError('Failed to unfollow user.')
    } finally {
      setBusyId(null)
    }
  }

  if (loading) return <p>Loading explorers...</p>

  const otherUsers = users.filter((u) => u.id !== myId)

  return (
    <section className="explorers-page">
      <h1>Explorers</h1>
      {error && <p className="message-error">{error}</p>}

      <div className="card">
        <div className="section-header">
          <div>
            <p className="section-eyebrow">Suggested for you</p>
            <h2>Recommended profiles</h2>
            <p className="section-note">
              Based on who the people you follow are following.
            </p>
          </div>
        </div>
        {recommendations.length === 0 ? (
          <p className="empty-state">
            No recommendations yet — follow a few explorers and we will suggest more.
          </p>
        ) : (
          <ul className="user-list">
            {recommendations.map((rec) => (
              <li key={rec.id} className="user-row">
                <div>
                  <strong>{usernameOf(rec.id)}</strong>
                  <p className="user-row-meta">
                    Followed by {rec.mutualCount}{' '}
                    {rec.mutualCount === 1 ? 'person' : 'people'} you follow
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => void handleFollow(rec.id)}
                  disabled={busyId === rec.id}
                >
                  {busyId === rec.id ? 'Following...' : 'Follow'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="card">
        <div className="section-header">
          <div>
            <p className="section-eyebrow">Community</p>
            <h2>All explorers</h2>
          </div>
          <span className="pill">Following {followingIds.length}</span>
        </div>
        {otherUsers.length === 0 ? (
          <p className="empty-state">No other explorers registered yet.</p>
        ) : (
          <ul className="user-list">
            {otherUsers.map((user) => {
              const followed = followingSet.has(user.id)
              return (
                <li key={user.id} className="user-row">
                  <div>
                    <strong>{user.username}</strong>
                    <p className="user-row-meta">{user.role}</p>
                  </div>
                  {followed ? (
                    <button
                      type="button"
                      className="button-secondary"
                      onClick={() => void handleUnfollow(user.id)}
                      disabled={busyId === user.id}
                    >
                      {busyId === user.id ? 'Unfollowing...' : 'Unfollow'}
                    </button>
                  ) : (
                    <button
                      type="button"
                      onClick={() => void handleFollow(user.id)}
                      disabled={busyId === user.id}
                    >
                      {busyId === user.id ? 'Following...' : 'Follow'}
                    </button>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </div>

      <div className="card">
        <div className="section-header">
          <div>
            <p className="section-eyebrow">Your audience</p>
            <h2>My followers</h2>
          </div>
          <span className="pill">{followerIds.length}</span>
        </div>
        {followerIds.length === 0 ? (
          <p className="empty-state">Nobody follows you yet.</p>
        ) : (
          <ul className="user-list">
            {followerIds.map((id) => (
              <li key={id} className="user-row">
                <strong>{usernameOf(id)}</strong>
                {!followingSet.has(id) && (
                  <button
                    type="button"
                    onClick={() => void handleFollow(id)}
                    disabled={busyId === id}
                  >
                    {busyId === id ? 'Following...' : 'Follow back'}
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}
