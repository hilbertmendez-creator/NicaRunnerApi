import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios'
import type { AuthResponse } from './types'

const ACCESS_TOKEN_STORAGE_KEY = 'nicarunner.token'
const REFRESH_TOKEN_STORAGE_KEY = 'nicarunner.refreshToken'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
})

// Instancia SIN interceptores para la llamada a /auth/refresh y /auth/logout —
// evita loops infinitos si el /refresh devuelve 401 (el interceptor no re-entra
// en sí mismo). Comparte baseURL con apiClient.
const refreshClient = axios.create({ baseURL: apiClient.defaults.baseURL })

export function getStoredToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY)
}

export function setStoredToken(token: string | null) {
  if (token) {
    localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token)
  } else {
    localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY)
  }
}

export function getStoredRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY)
}

export function setStoredRefreshToken(token: string | null) {
  if (token) {
    localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, token)
  } else {
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY)
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

// Endpoints que NO deben disparar el flujo de refresh: el propio /refresh y el
// /logout (si /refresh está en curso y /logout también manda 401, no queremos
// arrancar otro refresh), y los flows de auth pública que 401 significa
// credenciales inválidas, no token expirado.
const AUTH_ENDPOINTS_SIN_REFRESH = [
  '/auth/refresh',
  '/auth/logout',
  '/auth/login',
  '/auth/register',
  '/auth/google-login',
  '/auth/forgot-password',
  '/auth/reset-password',
]

function isAuthEndpointSinRefresh(url: string | undefined): boolean {
  if (!url) return false
  return AUTH_ENDPOINTS_SIN_REFRESH.some((endpoint) => url.includes(endpoint))
}

// Cola de requests que esperan a que termine el /refresh en curso. Cuando el
// refresh termina, todos se resuelven con el nuevo token (o rechazan si falló).
// Sin esto, N requests concurrentes cuando el access token expira dispararían
// N refreshes en paralelo — el backend detectaría replay del segundo refresh en
// adelante y mataría la familia entera (PR #13).
let refreshPromise: Promise<string | null> | null = null

async function performRefresh(): Promise<string | null> {
  const refreshToken = getStoredRefreshToken()
  if (!refreshToken) return null

  try {
    const { data } = await refreshClient.post<AuthResponse>('/auth/refresh', { refreshToken })
    setStoredToken(data.token)
    setStoredRefreshToken(data.refreshToken)
    return data.token
  } catch {
    // Refresh falló — token expirado, revocado, o familia matada por replay.
    // Limpiamos todo del lado cliente y dejamos que el hard-logout redirija.
    setStoredToken(null)
    setStoredRefreshToken(null)
    return null
  }
}

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retriedAfterRefresh?: boolean
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status
    const originalConfig = error.config as RetriableConfig | undefined

    // Los 401 sobre endpoints de auth pública NO se refreshean — significan
    // credenciales malas, no expiración. Cliente los ve como error normal.
    if (
      status !== 401 ||
      !originalConfig ||
      isAuthEndpointSinRefresh(originalConfig.url) ||
      originalConfig._retriedAfterRefresh
    ) {
      // Si es 401 en un endpoint autenticado no-refresheable, o si ya
      // reintentamos y volvió a fallar, hard-logout.
      if (status === 401 && !isAuthEndpointSinRefresh(originalConfig?.url)) {
        setStoredToken(null)
        setStoredRefreshToken(null)
        onUnauthorized?.()
      }
      return Promise.reject(error)
    }

    // Si ya hay un refresh en curso, esperamos ese en vez de disparar otro.
    if (!refreshPromise) {
      refreshPromise = performRefresh().finally(() => {
        // Damos un tick para que todos los awaits pendientes vean el resultado
        // antes de que otro 401 futuro dispare otro refresh nuevo.
        setTimeout(() => {
          refreshPromise = null
        }, 0)
      })
    }

    const newToken = await refreshPromise
    if (!newToken) {
      onUnauthorized?.()
      return Promise.reject(error)
    }

    // Reintentar el request original con el token nuevo. Marcamos como
    // "reintentado" para que si vuelve a fallar con 401 no entremos en loop.
    originalConfig._retriedAfterRefresh = true
    originalConfig.headers.Authorization = `Bearer ${newToken}`
    return apiClient(originalConfig as AxiosRequestConfig)
  },
)
