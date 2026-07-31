import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import axios from 'axios'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'
import { NicaRunnerLogo } from './NicaRunnerLogo'
import { PasswordInput } from '../components/PasswordInput'

const CREDENCIALES_INVALIDAS = 'Usuario o contraseña incorrectos'

/**
 * Traduce el fallo del login a un mensaje honesto. Antes cualquier excepción se
 * reportaba como credenciales inválidas, así que una API caída, un problema de
 * red o un 500 mandaban al usuario a reescribir una contraseña que estaba bien
 * — y ocultaban la causa real a quien tuviera que diagnosticarlo.
 *
 * Para el 401 seguimos mostrando el mensaje genérico que manda el backend: es
 * deliberadamente ambiguo entre contraseña incorrecta, cuenta bloqueada y cuenta
 * inactiva para no permitir enumerar usuarios. Eso no se toca acá.
 */
function describirErrorDeLogin(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'No pudimos iniciar sesión. Intentá de nuevo.'

  if (!error.response) {
    return 'No pudimos conectar con el servidor. Revisá tu conexión e intentá de nuevo.'
  }

  const { status, data } = error.response
  const detalle = typeof data?.detail === 'string' ? data.detail.trim() : ''

  if (status === 401) return detalle || CREDENCIALES_INVALIDAS
  if (status === 429) return detalle || 'Demasiados intentos. Esperá un momento e intentá de nuevo.'
  if (status >= 500) return 'El servidor tuvo un problema. Intentá de nuevo en unos minutos.'

  return detalle || 'No pudimos iniciar sesión. Intentá de nuevo.'
}

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  // user-auth: "Unified Identifier Login" — el campo acepta email o alias, ya no
  // solo email (design.md §3.1/§4.1).
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(identifier, password)
      navigate('/', { replace: true })
    } catch (err) {
      setError(describirErrorDeLogin(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen lg:flex-row" style={{ background: 'var(--bg-app)' }}>
      <div
        className="relative hidden w-1/2 items-center justify-center overflow-hidden lg:flex"
        style={{ background: 'var(--bg-sidebar)' }}
      >
        <div
          className="absolute inset-0"
          style={{
            background: 'radial-gradient(circle at 50% 45%, rgba(126,20,255,0.35), transparent 60%)',
          }}
          aria-hidden="true"
        />
        <div className="relative flex flex-col items-center gap-4 text-center">
          <NicaRunnerLogo className="h-36 w-36" />
          <span className="text-2xl font-semibold" style={{ color: 'var(--text-hi)' }}>
            nicaRunner
          </span>
          <span className="text-sm" style={{ color: 'var(--text-lo)' }}>
            Back Office de administración de carreras
          </span>
        </div>
      </div>

      <div className="flex w-full items-center justify-center px-6 py-12 lg:w-1/2">
        <form
          onSubmit={handleSubmit}
          className="w-full max-w-sm p-8"
          style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
        >
          <div className="mb-8 flex flex-col items-center gap-2 lg:hidden">
            <NicaRunnerLogo className="h-16 w-16" />
            <span className="text-lg font-semibold" style={{ color: 'var(--text-hi)' }}>
              nicaRunner
            </span>
          </div>

          <h1 className="mb-6 text-xl font-semibold" style={{ color: 'var(--text-hi)' }}>
            Back Office
          </h1>

          <Label htmlFor="identifier">Email o alias</Label>
          <Input
            id="identifier"
            type="text"
            autoComplete="username"
            required
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            className="mb-4 w-full"
          />

          <Label htmlFor="password">Contraseña</Label>
          <PasswordInput
            id="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full"
          />
          <div className="mb-4 mt-1 text-right">
            <Link to="/forgot-password" className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
              ¿Olvidaste tu contraseña?
            </Link>
          </div>

          {error && (
            <p className="mb-4 text-sm" style={{ color: 'var(--badge-er-text)' }}>
              {error}
            </p>
          )}

          <Button type="submit" variant="primary" disabled={submitting} className="w-full">
            {submitting ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>
      </div>
    </div>
  )
}
