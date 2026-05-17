import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { parseAuthUser } from '../shared/auth'

export function AppLayout() {
  const { isAuthenticated, logout, token } = useAuth()
  const navigate = useNavigate()
  const authUser = parseAuthUser(token)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      <header className="top-nav">
        <nav>
          <Link to="/tours">All Tours</Link>
          {authUser.role === 'Guide' && <Link to="/tours/me">My Tours</Link>}
          {authUser.role === 'Guide' && <Link to="/tours/new">Create Tour</Link>}
          <Link to="/simulator">Simulator</Link>
        </nav>
        <div>
          {isAuthenticated ? (
            <button type="button" onClick={handleLogout}>
              Logout
            </button>
          ) : (
            <>
              <Link to="/login">Login</Link>
              {' | '}
              <Link to="/register">Register</Link>
            </>
          )}
        </div>
      </header>

      <main className="page-container">
        <Outlet />
      </main>
    </div>
  )
}
