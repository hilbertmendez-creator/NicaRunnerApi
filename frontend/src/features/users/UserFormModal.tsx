import { useState, type FormEvent } from 'react'
import axios from 'axios'
import toast from 'react-hot-toast'
import { createUser, updateUser } from '../../api/endpoints'
import type { UserDto, UserRole } from '../../api/types'
import { Modal, Button, Label, Input, Select } from '@nicarunner/ui'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']
const ALIAS_PATTERN = '^[a-z0-9._-]{3,30}$'

interface UserFormModalProps {
  user: UserDto | null
  onClose: () => void
  onSaved: () => void
}

export function UserFormModal({ user, onClose, onSaved }: UserFormModalProps) {
  const [email, setEmail] = useState('')
  const [nombre, setNombre] = useState(user?.nombre ?? '')
  const [role, setRole] = useState<UserRole>('Lector')
  // user-management: "Admin-Editable Alias" — namespace amplio (^[a-z0-9._-]{3,30}$),
  // se re-valida en el backend en cada edición.
  const [username, setUsername] = useState(user?.username ?? '')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  // user-management: "Alias auto-generated on create" — el alias no existe hasta
  // que el servidor responde; se muestra aquí en vez de cerrar el modal de inmediato,
  // porque no hay ningún otro lugar donde el admin lo vea antes del primer refresh.
  const [createdUser, setCreatedUser] = useState<UserDto | null>(null)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (user) {
        await updateUser(user.id, { nombre, username: username.trim().toLowerCase() })
        toast.success('Usuario actualizado correctamente')
        onSaved()
      } else {
        const created = await createUser({ email, nombre, role })
        toast.success('Usuario creado correctamente')
        setCreatedUser(created)
      }
    } catch (err) {
      // user-management: "Admin edits alias to a value already taken" — el 409
      // se distingue del resto de errores para no confundir al admin con el
      // mensaje genérico de nombre/email.
      if (user && axios.isAxiosError(err) && err.response?.status === 409) {
        const msg = `El alias "${username.trim().toLowerCase()}" ya está en uso por otro usuario.`
        setError(msg)
        toast.error(msg)
      } else {
        const msg = user
          ? 'No se pudo actualizar el usuario. Verificá el nombre y el formato del alias.'
          : 'No se pudo crear el usuario. Verificá que el email no esté ya registrado.'
        setError(msg)
        toast.error(msg)
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (createdUser) {
    return (
      <Modal onClose={onSaved} labelledBy="user-form-title">
        <h2 id="user-form-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
          Usuario creado
        </h2>
        <p className="mb-3 text-sm" style={{ color: 'var(--text-lo)' }}>
          Se creó <strong style={{ color: 'var(--text-hi)' }}>{createdUser.nombre}</strong> con el alias generado:
        </p>
        <p
          className="mb-4 border px-3 py-2 font-mono text-sm"
          style={{
            borderColor: 'var(--bd)',
            background: 'var(--bg-input)',
            color: 'var(--text-hi)',
            borderRadius: 'var(--radius-btn)',
          }}
          data-testid="generated-alias"
        >
          {createdUser.username ?? '(sin alias — verifica la configuración del backend)'}
        </p>
        <div className="flex justify-end">
          <Button type="button" variant="primary" onClick={onSaved}>
            Entendido
          </Button>
        </div>
      </Modal>
    )
  }

  return (
    <Modal onClose={onClose} labelledBy="user-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="user-form-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
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

        {user && (
          <>
            <Label htmlFor="user-username">Alias</Label>
            <Input
              id="user-username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              minLength={3}
              maxLength={30}
              pattern={ALIAS_PATTERN}
              title="3-30 caracteres: minúsculas, números, puntos, guiones o guiones bajos"
              className="mb-3 w-full"
            />
          </>
        )}

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
            <p className="mb-3 text-xs" style={{ color: 'var(--text-xs)' }}>
              El alias de acceso se genera automáticamente a partir del nombre.
            </p>
          </>
        )}

        {error && (
          <p className="mb-3 text-sm" style={{ color: 'var(--badge-er-text)' }}>
            {error}
          </p>
        )}

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
