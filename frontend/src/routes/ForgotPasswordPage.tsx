import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { forgotPassword } from '../api/endpoints'
import { Button, Label, Input } from '@nicarunner/ui'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    try {
      await forgotPassword({ email })
    } finally {
      // Siempre mostramos el mismo mensaje, exista o no la cuenta
      // (evita revelar qué correos están registrados).
      setSubmitting(false)
      setSubmitted(true)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-6" style={{ background: 'var(--bg-app)' }}>
      <div
        className="w-full max-w-sm p-8"
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--bd-card)',
          borderRadius: 'var(--radius-card)',
          boxShadow: 'var(--shadow-md)',
        }}
      >
        <h1 className="mb-2 text-xl font-semibold" style={{ color: 'var(--text-hi)' }}>
          Recuperar contraseña
        </h1>

        {submitted ? (
          <>
            <p className="mb-6 text-sm" style={{ color: 'var(--text-lo)' }}>
              Si el correo <strong style={{ color: 'var(--text-hi)' }}>{email}</strong> está registrado,
              enviamos un enlace para restablecer la contraseña. Revisá tu bandeja de entrada.
            </p>
            <Link to="/login" className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
              Volver al login
            </Link>
          </>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="mb-4 text-sm" style={{ color: 'var(--text-lo)' }}>
              Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña.
            </p>

            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mb-4 w-full"
            />

            <Button type="submit" variant="primary" disabled={submitting} className="mb-3 w-full">
              {submitting ? 'Enviando...' : 'Enviar enlace'}
            </Button>
            <Link
              to="/login"
              className="block text-center text-sm hover:underline"
              style={{ color: 'var(--accent)' }}
            >
              Volver al login
            </Link>
          </form>
        )}
      </div>
    </div>
  )
}
