import { useState, type FormEvent } from 'react'
import { changePassword } from '../api/endpoints'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'

export function ChangePasswordPage() {
  const { clearMustChangePassword, logout } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (newPassword !== confirmPassword) {
      setError('Las contraseñas nuevas no coinciden.')
      return
    }
    if (newPassword.length < 6) {
      setError('La nueva contraseña debe tener al menos 6 caracteres.')
      return
    }

    setSubmitting(true)
    try {
      await changePassword({ currentPassword, newPassword })
      clearMustChangePassword()
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
        <Input
          id="current-password"
          type="password"
          required
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          className="mb-4 w-full"
        />

        <Label htmlFor="new-password">Nueva contraseña</Label>
        <Input
          id="new-password"
          type="password"
          required
          minLength={6}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="mb-4 w-full"
        />

        <Label htmlFor="confirm-password">Confirmar nueva contraseña</Label>
        <Input
          id="confirm-password"
          type="password"
          required
          minLength={6}
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
