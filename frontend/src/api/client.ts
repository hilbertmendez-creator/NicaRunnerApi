import axios from 'axios'

// El JWT y el refresh token viajan en cookies httpOnly (nr_at / nr_rt) que el
// backend setea en login/refresh/logout — el cliente web ya no los lee ni
// los guarda (localStorage era vulnerable a robo vía XSS). withCredentials
// hace que el browser adjunte esas cookies en cada request.
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  withCredentials: true,
})

/**
 * Saca el `detail` de un ProblemDetails de la API, con fallback.
 *
 * Los catch de los componentes anotaban `err: any` para poder escribir
 * `err.response?.data?.detail` — un `any` explícito que apaga el chequeo de
 * tipos en toda la expresión. Acá el error entra como `unknown` y se estrecha:
 * `isAxiosError` descarta lo que no sea un error de red, y el `typeof` evita
 * devolver un objeto o un número como si fuera texto de UI (mismo chequeo que
 * ya hacía LoginPage).
 */
export function apiErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const detail = (err.response?.data as { detail?: unknown } | undefined)?.detail
    if (typeof detail === 'string' && detail.trim()) return detail.trim()
  }
  return fallback
}

const CSRF_RESPONSE_HEADER = 'x-csrf-token'
const MUTATING_METHODS = new Set(['post', 'put', 'patch', 'delete'])

// El backend (Render) y este frontend (Vercel) viven en dominios distintos:
// JS de acá no puede leer la cookie nr_csrf vía document.cookie aunque no sea
// httpOnly (las cookies solo son legibles por script del mismo dominio que
// las seteó). Por eso el backend además la manda como header en cada
// respuesta autenticada — la guardamos en memoria y la reenviamos nosotros.
let csrfToken: string | null = null

apiClient.interceptors.response.use((response) => {
  const headerValue = response.headers[CSRF_RESPONSE_HEADER]
  if (headerValue) csrfToken = headerValue
  return response
})

// Double-submit CSRF: el backend valida que este header coincida con el
// valor de la cookie nr_csrf en requests mutantes autenticados por cookie.
apiClient.interceptors.request.use((config) => {
  const method = config.method?.toLowerCase()
  if (method && MUTATING_METHODS.has(method) && csrfToken) {
    config.headers['X-CSRF-Token'] = csrfToken
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
let pendingQueue: Array<() => void> = []

function drainQueue() {
  pendingQueue.forEach((resolve) => resolve())
  pendingQueue = []
}

function clearSession() {
  pendingQueue = []
  onUnauthorized?.()
}

// Endpoints anónimos donde un 401 significa "estas credenciales no sirven", no
// "tu sesión expiró". Refrescar ahí no tiene sentido: no hay sesión que
// renovar. Antes el login fallido caía en el retry genérico, así que cada
// contraseña equivocada disparaba un POST /auth/refresh inútil y un
// clearSession() antes de rechazar. Lo mismo con un link de reseteo vencido.
// /auth/refresh está en la lista para cortar el reintento infinito.
const NO_REFRESH_PATHS = [
  '/auth/refresh',
  '/auth/login',
  '/auth/google-login',
  '/auth/reset-password',
]

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config

    // Don't attempt refresh if: not a 401, already retried, or the failing
    // request is one where refreshing can't help.
    if (
      error.response?.status !== 401 ||
      !original ||
      original._retry ||
      NO_REFRESH_PATHS.some((path) => original.url?.includes(path))
    ) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise<void>((resolve) => {
        pendingQueue.push(resolve)
      }).then(() => apiClient(original))
    }

    original._retry = true
    isRefreshing = true

    try {
      // Sin body: el refresh token viaja en la cookie httpOnly nr_rt.
      await apiClient.post('/auth/refresh')
      drainQueue()
      return apiClient(original)
    } catch {
      clearSession()
      return Promise.reject(error)
    } finally {
      isRefreshing = false
    }
  },
)
