import axios from 'axios'

const TOKEN_KEY = 'nicarunner.token'
const REFRESH_TOKEN_KEY = 'nicarunner.refresh_token'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
})

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setStoredToken(token: string | null) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token)
  } else {
    localStorage.removeItem(TOKEN_KEY)
  }
}

export function getStoredRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setStoredRefreshToken(token: string | null) {
  if (token) {
    localStorage.setItem(REFRESH_TOKEN_KEY, token)
  } else {
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  }
}

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export type UnauthorizedHandler = () => void
let onUnauthorized: UnauthorizedHandler | null = null

export function setUnauthorizedHandler(handler: UnauthorizedHandler) {
  onUnauthorized = handler
}

// Deduplication: if multiple requests 401 simultaneously, only one refresh
// call goes out. The rest queue and replay once the refresh resolves.
let isRefreshing = false
let pendingQueue: Array<(token: string) => void> = []

function drainQueue(token: string) {
  pendingQueue.forEach((resolve) => resolve(token))
  pendingQueue = []
}

function clearSession() {
  setStoredToken(null)
  setStoredRefreshToken(null)
  pendingQueue = []
  onUnauthorized?.()
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config

    // Don't attempt refresh if: not a 401, already retried, or the failing
    // request IS the refresh call (prevents infinite retry loop).
    if (
      error.response?.status !== 401 ||
      original._retry ||
      original.url?.includes('/auth/refresh')
    ) {
      return Promise.reject(error)
    }

    const refreshToken = getStoredRefreshToken()
    if (!refreshToken) {
      clearSession()
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise<string>((resolve) => {
        pendingQueue.push(resolve)
      }).then((newToken) => {
        original.headers.Authorization = `Bearer ${newToken}`
        return apiClient(original)
      })
    }

    original._retry = true
    isRefreshing = true

    try {
      const { data } = await apiClient.post('/auth/refresh', { refreshToken })
      setStoredToken(data.token)
      setStoredRefreshToken(data.refreshToken)
      drainQueue(data.token)
      original.headers.Authorization = `Bearer ${data.token}`
      return apiClient(original)
    } catch {
      clearSession()
      return Promise.reject(error)
    } finally {
      isRefreshing = false
    }
  },
)
