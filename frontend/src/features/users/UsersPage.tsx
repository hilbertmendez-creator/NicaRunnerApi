import { useEffect, useState } from 'react'
import { getUserAudit, getUsers, unlockUser, updateUser } from '../../api/endpoints'
import type { UserDto, UserRole } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, Select } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { UserFormModal } from './UserFormModal'
import { EntityAuditHistory } from '../../components/EntityAuditHistory'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [editing, setEditing] = useState<UserDto | null>(null)
  const [auditingUser, setAuditingUser] = useState<UserDto | null>(null)
  const [unlockingId, setUnlockingId] = useState<number | null>(null)

  function reload() {
    setLoading(true)
    getUsers()
      .then(setUsers)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [])

  async function handleRoleChange(target: UserDto, role: UserRole) {
    await updateUser(target.id, { role })
    reload()
  }

  async function handleToggleActive(target: UserDto) {
    await updateUser(target.id, { isActive: !target.isActive })
    reload()
  }

  // login-lockout: "Admin Unlock". UserDto no expone LockedUntilUtc/FailedLoginCount
  // (gap del contrato actual — ver Deviations), así que el botón se muestra siempre
  // en vez de solo para cuentas bloqueadas; el backend trata el desbloqueo de una
  // cuenta ya desbloqueada como no-op seguro (UserManagementService.UnlockAsync).
  async function handleUnlock(target: UserDto) {
    setUnlockingId(target.id)
    try {
      await unlockUser(target.id)
      reload()
    } finally {
      setUnlockingId(null)
    }
  }

  const columns: Column<UserDto>[] = [
    { header: 'Email', render: (u) => u.email },
    { header: 'Nombre', render: (u) => u.nombre },
    { header: 'Alias', render: (u) => u.username ?? '—' },
    {
      header: 'Rol',
      render: (u) => {
        const isSelf = u.id === currentUser?.userId
        return (
          <Select
            value={u.role}
            disabled={isSelf}
            onChange={(e) => handleRoleChange(u, e.target.value as UserRole)}
          >
            {ROLE_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </Select>
        )
      },
    },
    {
      header: 'Estado',
      render: (u) => (u.isActive ? 'Activo' : 'Inactivo'),
    },
    {
      header: 'Creado',
      render: (u) => new Date(u.createdAt).toLocaleDateString(),
    },
    {
      header: '',
      render: (u) => {
        const isSelf = u.id === currentUser?.userId
        return (
          <div className="flex gap-2">
            <Button size="sm" onClick={() => setEditing(u)}>
              Editar
            </Button>
            <Button size="sm" onClick={() => setAuditingUser(u)}>
              Historial
            </Button>
            <Button
              size="sm"
              variant="info"
              disabled={unlockingId === u.id}
              onClick={() => handleUnlock(u)}
              title="Limpia el bloqueo por intentos fallidos, si lo hubiera."
            >
              {unlockingId === u.id ? 'Desbloqueando...' : 'Desbloquear'}
            </Button>
            <Button
              size="sm"
              variant={u.isActive ? 'destructive' : 'primary'}
              disabled={isSelf}
              onClick={() => handleToggleActive(u)}
            >
              {u.isActive ? 'Desactivar' : 'Activar'}
            </Button>
          </div>
        )
      },
    },
  ]

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-end">
        <Button variant="primary" onClick={() => setShowCreate(true)}>
          Nuevo usuario
        </Button>
      </div>

      {loading && <LoadingText message="Cargando usuarios..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={users}
          rowKey={(u) => u.id}
          emptyState={<EmptyState message="Todavía no hay usuarios de backoffice." />}
        />
      )}

      {showCreate && (
        <UserFormModal
          user={null}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <UserFormModal
          user={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}

      {auditingUser && (
        <EntityAuditHistory
          title={`Auditoría — usuario ${auditingUser.email}`}
          load={() => getUserAudit(auditingUser.id)}
          onClose={() => setAuditingUser(null)}
        />
      )}
    </div>
  )
}
