import { useEffect, useState } from 'react'
import { blockUser, getAllUsers, unblockUser, type UserAccount } from '../api/adminApi'

export function AdminUsersPage() {
  const [users, setUsers] = useState<UserAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const data = await getAllUsers()
        setUsers(data)
      } catch {
        setError('Failed to load users. Make sure you are logged in as Admin.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [])

  const handleToggleBlock = async (user: UserAccount) => {
    setBusyId(user.id)
    setError(null)
    try {
      const updated = user.isBlocked ? await unblockUser(user.id) : await blockUser(user.id)
      setUsers((prev) => prev.map((u) => (u.id === updated.id ? updated : u)))
    } catch {
      setError(`Failed to ${user.isBlocked ? 'unblock' : 'block'} ${user.username}.`)
    } finally {
      setBusyId(null)
    }
  }

  if (loading) return <p>Loading users...</p>

  return (
    <section className="admin-users-page">
      <div className="section-header">
        <div>
          <p className="section-eyebrow">Administration</p>
          <h1>User Accounts</h1>
        </div>
        <span className="pill">{users.length} accounts</span>
      </div>
      {error && <p className="message-error">{error}</p>}
      <div className="card">
        <table className="users-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Email</th>
              <th>Role</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>
                  <strong>{user.username}</strong>
                </td>
                <td>{user.email}</td>
                <td>
                  <span className="pill">{user.role}</span>
                </td>
                <td>
                  {user.isBlocked ? (
                    <span className="status-blocked">Blocked</span>
                  ) : (
                    <span className="status-active">Active</span>
                  )}
                </td>
                <td className="users-table-actions">
                  {user.role !== 'Admin' && (
                    <button
                      type="button"
                      className={user.isBlocked ? 'button-secondary' : 'button-danger'}
                      onClick={() => void handleToggleBlock(user)}
                      disabled={busyId === user.id}
                    >
                      {busyId === user.id ? '...' : user.isBlocked ? 'Unblock' : 'Block'}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}
