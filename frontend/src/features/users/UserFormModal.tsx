import { useState, type FormEvent } from 'react'
import { createUser, updateUser } from '../../api/endpoints'
import type { UserDto, UserRole } from '../../api/types'
import { Modal, Button, Label, Input, Select } from '@nicarunner/ui'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']

interface UserFormModalProps {
  user: UserDto | null
  onClose: () => void
  onSaved: () => void
}

export function UserFormModal({ user, onClose, onSaved }: UserFormModalProps) {
  const [email, setEmail] = useState('')
  const [nombre, setNombre] = useState(user?.nombre ?? '')
  const [role, setRole] = useState<UserRole>('Lector')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (user) {
        await updateUser(user.id, { nombre })
      } else {
        await createUser({ email, nombre, role })
      }
      onSaved()
    } catch {
      setError(
        user
          ? 'No se pudo actualizar el nombre. Verifica que no esté vacío.'
          : 'No se pudo crear el usuario. Verifica que el email no esté ya registrado.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="user-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="user-form-title" className="mb-4 text-base font-semibold text-zinc-900">
          {user ? 'Editar usuario' : 'Nuevo usuario'}
        </h2>

        {!user && (
          <>
            <Label htmlFor="user-email">Email</Label>
            <Input
              id="user-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="mb-3 w-full"
            />
          </>
        )}

        <Label htmlFor="user-nombre">Nombre</Label>
        <Input
          id="user-nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          className="mb-3 w-full"
        />

        {!user && (
          <>
            <Label htmlFor="user-role">Rol</Label>
            <Select
              id="user-role"
              value={role}
              onChange={(e) => setRole(e.target.value as UserRole)}
              className="mb-3 w-full"
            >
              {ROLE_OPTIONS.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </Select>
          </>
        )}

        {error && <p className="mb-3 text-sm text-critical-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? 'Guardando...' : user ? 'Guardar' : 'Crear'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
