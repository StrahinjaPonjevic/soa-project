import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'

type AuthContextValue = {
  token: string | null
  isAuthenticated: boolean
  login: (token: string) => void
  logout: () => void
}

const TOKEN_STORAGE_KEY = 'explorer_auth_token'

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_STORAGE_KEY))

  const value = useMemo<AuthContextValue>(() => {
    return {
      token,
      isAuthenticated: !!token,
      login: (newToken: string) => {
        localStorage.setItem(TOKEN_STORAGE_KEY, newToken)
        setToken(newToken)
      },
      logout: () => {
        localStorage.removeItem(TOKEN_STORAGE_KEY)
        setToken(null)
      },
    }
  }, [token])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used inside AuthProvider')
  }

  return ctx
}
