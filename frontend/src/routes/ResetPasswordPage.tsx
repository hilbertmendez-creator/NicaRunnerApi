import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { resetPassword } from '../api/endpoints'
import { Button, Label } from '@nicarunner/ui'
import { isStrongPassword, PASSWORD_POLICY_HINT } from '../auth/passwordPolicy'
import { PasswordInput } from '../components/PasswordInput'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!isStrongPassword(newPassword)) {
      setError(PASSWORD_POLICY_HINT)
      return
    }

    if (newPassword !== confirmPassword) {
      setError('Las contraseñas no coinciden.')
      return
    }

    setSubmitting(true)
    try {
      await resetPassword({ token, newPassword })
      navigate('/login', { replace: true })
    } catch {
      setError('El enlace no es válido o ya expiró. Solicita uno nuevo.')
    } finally {
      setSubmitting(false)
    }
  }

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-100">
        <div className="w-full max-w-sm rounded-lg bg-white p-8 text-center shadow-md">
          <p className="mb-4 text-sm text-gray-700">Este enlace de recuperación no es válido.</p>
          <Link to="/forgot-password" className="text-sm text-blue-700 hover:underline">
            Solicitar un enlace nuevo
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg bg-white p-8 shadow-md">
        <h1 className="mb-6 text-xl font-semibold text-gray-900">Restablecer contraseña</h1>

        <Label htmlFor="new-password">Nueva contraseña</Label>
        <PasswordInput
          id="new-password"
          required
          minLength={8}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="mb-1 w-full"
        />
        <p className="mb-4 text-xs text-gray-500">{PASSWORD_POLICY_HINT}</p>

        <Label htmlFor="confirm-password">Confirmar contraseña</Label>
        <PasswordInput
          id="confirm-password"
          required
          minLength={8}
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          className="mb-4 w-full"
        />

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="w-full">
          {submitting ? 'Guardando...' : 'Restablecer contraseña'}
        </Button>
      </form>
    </div>
  )
}
