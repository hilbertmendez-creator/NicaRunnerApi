import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'

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
    <div className="flex min-h-screen items-center justify-center" style={{ background: 'var(--bg-app)' }}>
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm p-8"
        style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
      >
        <h1 className="mb-6 text-xl font-semibold" style={{ color: 'var(--text-hi)' }}>nicaRunner — Back Office</h1>

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
          className="mb-4 w-full"
        />

        {error && <p className="mb-4 text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="w-full">
          {submitting ? 'Ingresando...' : 'Ingresar'}
        </Button>
      </form>
    </div>
  )
}
