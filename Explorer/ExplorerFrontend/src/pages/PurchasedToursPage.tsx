import { useEffect, useState } from 'react'
import { getPurchaseTokens, type PurchaseToken } from '../api/purchaseApi'

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === 'object' && error !== null) {
    const maybeResponse = (error as { response?: { data?: { message?: string } } }).response
    const message = maybeResponse?.data?.message
    if (typeof message === 'string' && message.length > 0) {
      return message
    }
  }
  return fallback
}

export function PurchasedToursPage() {
  const [tokens, setTokens] = useState<PurchaseToken[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const loadedTokens = await getPurchaseTokens()
        setTokens(loadedTokens)
      } catch (err) {
        setError(getErrorMessage(err, 'Failed to load purchased tours.'))
      } finally {
        setLoading(false)
      }
    }
    void run()
  }, [])

  if (loading) return <p>Loading purchased tours...</p>
  if (error) return <p className="message-error">{error}</p>

  return (
    <section className="card">
      <div className="section-header">
        <div>
          <p className="section-eyebrow">Purchase</p>
          <h2>My Purchased Tours</h2>
        </div>
      </div>

      {tokens.length === 0 ? (
        <p className="empty-state">No purchased tours yet.</p>
      ) : (
        <ul className="cart-list">
          {tokens.map((entry) => (
            <li key={`${entry.tourId}-${entry.token}`} className="cart-item">
              <div>
                <strong>Tour #{entry.tourId}</strong>
                <p className="tour-card-meta">Purchased: {new Date(entry.purchasedAt).toLocaleString()}</p>
              </div>
              <span className="pill">{entry.token}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
