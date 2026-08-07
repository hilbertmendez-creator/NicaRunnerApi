import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { changePassword } from '../api/endpoints'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'

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
      navigate('/', { replace: true })
    } catch {
      setError('No se pudo cambiar la contraseña. Verifica la contraseña actual.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center" style={{ background: 'var(--bg-app)' }}>
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm p-8"
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--bd-card)',
          borderRadius: 'var(--radius-card)',
        }}
      >
        <h1 className="mb-2 text-xl font-semibold" style={{ color: 'var(--text-hi)' }}>Cambia tu contraseña</h1>
        <p className="mb-6 text-sm" style={{ color: 'var(--text-lo)' }}>
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

        {error && <p className="mb-4 text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="mb-3 w-full">
          {submitting ? 'Guardando...' : 'Cambiar contraseña'}
        </Button>
        <button type="button" onClick={logout} className="w-full text-sm hover:underline" style={{ color: 'var(--accent)' }}>
          Cerrar sesión
        </button>
      </form>
    </div>
  )
}
