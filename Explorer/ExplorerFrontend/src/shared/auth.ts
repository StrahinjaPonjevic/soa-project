type JwtPayload = {
  userId?: string
  unique_name?: string
  name?: string
  role?: string
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'?: string
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string
  exp?: number
}

export type AuthUser = {
  userId: number | null
  username: string | null
  role: string | null
}

export function parseAuthUser(token: string | null): AuthUser {
  if (!token) {
    return { userId: null, username: null, role: null }
  }

  const parts = token.split('.')
  if (parts.length < 2) {
    return { userId: null, username: null, role: null }
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=')
    const payload = JSON.parse(atob(padded)) as JwtPayload
    return {
      userId: payload.userId
        ? Number(payload.userId)
        : payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
          ? Number(payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'])
          : null,
      username:
        payload.unique_name ??
        payload.name ??
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        null,
      role:
        payload.role ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        null,
    }
  } catch {
    return { userId: null, username: null, role: null }
  }
}
