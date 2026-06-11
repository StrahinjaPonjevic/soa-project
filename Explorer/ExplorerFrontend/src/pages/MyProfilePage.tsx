import { useEffect, useState, type FormEvent } from 'react'
import { getMyProfile, updateMyProfile, type ProfileResponse } from '../api/profileApi'

export function MyProfilePage() {
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  const [saving, setSaving] = useState(false)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [profileImageUrl, setProfileImageUrl] = useState('')
  const [biography, setBiography] = useState('')
  const [motto, setMotto] = useState('')

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const data = await getMyProfile()
        setProfile(data)
      } catch {
        setError('Failed to load your profile.')
      } finally {
        setLoading(false)
      }
    }

    void run()
  }, [])

  const startEditing = () => {
    if (!profile) return
    setFirstName(profile.firstName)
    setLastName(profile.lastName)
    setProfileImageUrl(profile.profileImageUrl ?? '')
    setBiography(profile.biography ?? '')
    setMotto(profile.motto ?? '')
    setSuccess(null)
    setEditing(true)
  }

  const handleSave = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const updated = await updateMyProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        profileImageUrl: profileImageUrl.trim() || null,
        biography: biography.trim() || null,
        motto: motto.trim() || null,
      })
      setProfile(updated)
      setEditing(false)
      setSuccess('Profile updated.')
    } catch {
      setError('Failed to update profile.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p>Loading profile...</p>
  if (!profile) {
    return (
      <div className="card">
        <p className="empty-state">Profile not found.</p>
        {error && <p className="message-error">{error}</p>}
      </div>
    )
  }

  const displayName =
    [profile.firstName, profile.lastName].filter(Boolean).join(' ') || 'Unnamed explorer'

  return (
    <section className="profile-page">
      <h1>My Profile</h1>
      <div className="card">
        {!editing ? (
          <>
            <div className="profile-view">
              {profile.profileImageUrl ? (
                <img className="profile-avatar" src={profile.profileImageUrl} alt={displayName} />
              ) : (
                <div className="profile-avatar profile-avatar-placeholder">
                  {(profile.firstName?.[0] ?? '?').toUpperCase()}
                </div>
              )}
              <div>
                <h2>{displayName}</h2>
                {profile.motto && <p className="profile-motto">“{profile.motto}”</p>}
                <p className="profile-bio">
                  {profile.biography || <span className="empty-state">No biography yet.</span>}
                </p>
              </div>
            </div>
            {success && <p className="message-success">{success}</p>}
            {error && <p className="message-error">{error}</p>}
            <div className="inline-actions" style={{ marginTop: '1rem' }}>
              <button type="button" onClick={startEditing}>
                Edit profile
              </button>
            </div>
          </>
        ) : (
          <form className="stacked-form" onSubmit={(e) => void handleSave(e)}>
            <div className="form-grid compact-grid">
              <label>
                <span>First name</span>
                <input value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </label>
              <label>
                <span>Last name</span>
                <input value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </label>
            </div>
            <label>
              <span>Profile image URL</span>
              <input
                value={profileImageUrl}
                onChange={(e) => setProfileImageUrl(e.target.value)}
                placeholder="https://example.com/me.jpg"
              />
            </label>
            <label>
              <span>Motto (quote)</span>
              <input
                value={motto}
                onChange={(e) => setMotto(e.target.value)}
                placeholder="Not all those who wander are lost."
              />
            </label>
            <label>
              <span>Biography</span>
              <textarea
                value={biography}
                onChange={(e) => setBiography(e.target.value)}
                rows={5}
                placeholder="Tell other explorers about yourself..."
              />
            </label>
            {error && <p className="message-error">{error}</p>}
            <div className="inline-actions">
              <button type="submit" disabled={saving}>
                {saving ? 'Saving...' : 'Save changes'}
              </button>
              <button
                type="button"
                className="button-secondary"
                onClick={() => setEditing(false)}
              >
                Cancel
              </button>
            </div>
          </form>
        )}
      </div>
    </section>
  )
}
