import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'
import { NicaRunnerLogo } from './NicaRunnerLogo'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      navigate('/', { replace: true })
    } catch {
      setError('Email o contraseña incorrectos')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen bg-white lg:flex-row">
      <div className="relative hidden w-1/2 items-center justify-center overflow-hidden bg-slate-blue-900 lg:flex">
        <div
          className="absolute inset-0"
          style={{
            background: 'radial-gradient(circle at 50% 45%, rgba(126,20,255,0.35), transparent 60%)',
          }}
          aria-hidden="true"
        />
        <div className="relative flex flex-col items-center gap-4 text-center">
          <NicaRunnerLogo className="h-36 w-36" />
          <span className="text-2xl font-semibold text-white">nicaRunner</span>
          <span className="text-sm text-zinc-400">Back Office de administración de carreras</span>
        </div>
      </div>

      <div className="flex w-full items-center justify-center px-6 py-12 lg:w-1/2">
        <form onSubmit={handleSubmit} className="w-full max-w-sm">
          <div className="mb-8 flex flex-col items-center gap-2 lg:hidden">
            <NicaRunnerLogo className="h-16 w-16" />
            <span className="text-lg font-semibold text-slate-blue-900">nicaRunner</span>
          </div>

          <h1 className="mb-6 text-xl font-semibold text-gray-900">Back Office</h1>

          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="mb-4 w-full"
          />

          <Label htmlFor="password">Contraseña</Label>
          <Input
            id="password"
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full"
          />
          <div className="mb-4 mt-1 text-right">
            <Link to="/forgot-password" className="text-sm text-blue-700 hover:underline">
              ¿Olvidaste tu contraseña?
            </Link>
          </div>

          {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

          <Button type="submit" variant="primary" disabled={submitting} className="w-full">
            {submitting ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>
      </div>
    </div>
  )
}
