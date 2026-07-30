import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { changePassword } from '../api/endpoints'
import { useAuth } from '../auth/auth-context'
import { Button, Label } from '@nicarunner/ui'
import { isStrongPassword, PASSWORD_POLICY_HINT } from '../auth/passwordPolicy'
import { PasswordInput } from '../components/PasswordInput'

export function ChangePasswordPage() {
  const { clearMustChangePassword, logout } = useAuth()
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
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
      setError('Las contraseñas nuevas no coinciden.')
      return
    }

    setSubmitting(true)
    try {
      await changePassword({ currentPassword, newPassword })
      clearMustChangePassword()
      navigate('/', { replace: true })
    } catch {
      setError('No se pudo cambiar la contraseña. Verifica la contraseña actual.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg bg-white p-8 shadow-md">
        <h1 className="mb-2 text-xl font-semibold text-gray-900">Cambia tu contraseña</h1>
        <p className="mb-6 text-sm text-gray-600">
          Es tu primer inicio de sesión. Define una contraseña personal antes de continuar.
        </p>

        <Label htmlFor="current-password">Contraseña temporal</Label>
        <PasswordInput
          id="current-password"
          required
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          className="mb-4 w-full"
        />

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

        <Label htmlFor="confirm-password">Confirmar nueva contraseña</Label>
        <PasswordInput
          id="confirm-password"
          required
          minLength={8}
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          className="mb-4 w-full"
        />

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="mb-3 w-full">
          {submitting ? 'Guardando...' : 'Cambiar contraseña'}
        </Button>
        <button type="button" onClick={logout} className="w-full text-sm text-blue-700 hover:underline">
          Cerrar sesión
        </button>
      </form>
    </div>
  )
}
