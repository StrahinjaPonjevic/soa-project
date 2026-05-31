import { useEffect, useState } from 'react'
import { checkoutCart, getCart, removeFromCart, type CartResponse, type PurchaseToken } from '../api/purchaseApi'
import './CartPage.css'

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

export function CartPage() {
  const [cart, setCart] = useState<CartResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [checkoutTokens, setCheckoutTokens] = useState<PurchaseToken[]>([])
  const [checkingOut, setCheckingOut] = useState(false)
  const [removingTourId, setRemovingTourId] = useState<number | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        setError(null)
        const loaded = await getCart()
        setCart(loaded)
      } catch (err) {
        setError(getErrorMessage(err, 'Failed to load cart.'))
      } finally {
        setLoading(false)
      }
    }
    void run()
  }, [])

  const handleRemove = async (tourId: number) => {
    try {
      setActionError(null)
      setRemovingTourId(tourId)
      const updated = await removeFromCart(tourId)
      setCart(updated)
    } catch (err) {
      setActionError(getErrorMessage(err, 'Failed to remove item.'))
    } finally {
      setRemovingTourId(null)
    }
  }

  const handleCheckout = async () => {
    try {
      setActionError(null)
      setCheckingOut(true)
      const tokens = await checkoutCart()
      setCheckoutTokens(tokens)
      const refreshed = await getCart()
      setCart(refreshed)
    } catch (err) {
      setActionError(getErrorMessage(err, 'Checkout failed.'))
    } finally {
      setCheckingOut(false)
    }
  }

  if (loading) return <p>Loading cart...</p>
  if (error) return <p className="message-error">{error}</p>

  return (
    <section className="card">
      <div className="section-header">
        <div>
          <p className="section-eyebrow">Purchase</p>
          <h2>Shopping Cart</h2>
        </div>
      </div>

      {!cart || cart.items.length === 0 ? (
        <p className="empty-state">Cart is empty.</p>
      ) : (
        <>
          <ul className="cart-list">
            {cart.items.map((item) => (
              <li key={item.tourId} className="cart-item">
                <div>
                  <strong>{item.tourName}</strong>
                  <p className="tour-card-meta">Tour ID: {item.tourId}</p>
                </div>
                <div className="inline-actions">
                  <span className="pill">{item.price}</span>
                  <button
                    type="button"
                    className="button-danger"
                    disabled={removingTourId === item.tourId}
                    onClick={() => void handleRemove(item.tourId)}
                  >
                    {removingTourId === item.tourId ? 'Removing...' : 'Remove'}
                  </button>
                </div>
              </li>
            ))}
          </ul>

          <div className="cart-footer">
            <strong>Total: {cart.totalPrice}</strong>
            <button type="button" disabled={checkingOut} onClick={() => void handleCheckout()}>
              {checkingOut ? 'Processing...' : 'Checkout'}
            </button>
          </div>
        </>
      )}

      {actionError && <p className="message-error">{actionError}</p>}
      {checkoutTokens.length > 0 && (
        <div className="message-success">
          Checkout complete. Generated tokens: {checkoutTokens.map((token) => token.token).join(', ')}
        </div>
      )}
    </section>
  )
}
