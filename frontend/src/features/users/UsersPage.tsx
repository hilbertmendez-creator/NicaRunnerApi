import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { getUserActiveRaces, getUserAudit, getUsers, unlockUser, updateUser } from '../../api/endpoints'
import type { ActiveRaceSummary, UserDto, UserRole } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, Modal, Select } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { UserFormModal } from './UserFormModal'
import { EntityAuditHistory } from '../../components/EntityAuditHistory'
import { UserStatusBadge } from '../../components/StatusBadge'
import { pageTitle } from '../../theme/styles'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [editing, setEditing] = useState<UserDto | null>(null)
  const [auditingUser, setAuditingUser] = useState<UserDto | null>(null)
  const [unlockingId, setUnlockingId] = useState<number | null>(null)
  const [pendingDeactivation, setPendingDeactivation] = useState<{
    user: UserDto
    races: ActiveRaceSummary[]
  } | null>(null)

  const [pageIndex, setPageIndex] = useState(1) // 1-based; DataTable contract
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 50

  function reload() {
    setLoading(true)
    getUsers(pageSize, (pageIndex - 1) * pageSize)
      .then((res) => {
        setUsers(res.items)
        setTotalCount(res.totalCount)
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [pageIndex])

  async function handleRoleChange(target: UserDto, role: UserRole) {
    try {
      await updateUser(target.id, { role })
      toast.success(`Rol actualizado a ${role}`)
      reload()
    } catch {
      toast.error('No se pudo actualizar el rol')
    }
  }

  // backoffice-user-status-toggle: "In-flight Capturista deactivation warning" (design.md
  // D6) — el pre-check solo aplica al desactivar (nunca al activar) y es advisory: si
  // encuentra carreras EnCurso donde el target es admin o juez, pide confirmación antes
  // del PATCH; si no encuentra ninguna, el PATCH sigue directo, igual que antes.
  async function handleToggleActive(target: UserDto) {
    if (target.isActive) {
      const activeRaces = await getUserActiveRaces(target.id)
      if (activeRaces.length > 0) {
        setPendingDeactivation({ user: target, races: activeRaces })
        return
      }
    }
    await applyToggle(target)
  }

  async function applyToggle(target: UserDto) {
    try {
      await updateUser(target.id, { isActive: !target.isActive })
      toast.success(target.isActive ? 'Usuario desactivado' : 'Usuario activado')
      reload()
    } catch {
      toast.error('No se pudo actualizar el estado del usuario')
    }
  }

  async function handleConfirmDeactivation() {
    if (!pendingDeactivation) return
    const target = pendingDeactivation.user
    setPendingDeactivation(null)
    await applyToggle(target)
  }

  // login-lockout: "Admin Unlock". UserDto no expone LockedUntilUtc/FailedLoginCount
  // (gap del contrato actual — ver Deviations), así que el botón se muestra siempre
  // en vez de solo para cuentas bloqueadas; el backend trata el desbloqueo de una
  // cuenta ya desbloqueada como no-op seguro (UserManagementService.UnlockAsync).
  async function handleUnlock(target: UserDto) {
    setUnlockingId(target.id)
    try {
      await unlockUser(target.id)
      toast.success('Usuario desbloqueado')
      reload()
    } catch {
      toast.error('No se pudo desbloquear el usuario')
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
            aria-label={`Rol de ${u.nombre}`}
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
      render: (u) => <UserStatusBadge isActive={u.isActive} />,
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
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold" style={pageTitle}>Usuarios</h1>
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
          pageIndex={pageIndex}
          pageCount={Math.ceil(totalCount / pageSize)}
          onPageChange={setPageIndex}
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

      {pendingDeactivation && (
        <Modal onClose={() => setPendingDeactivation(null)} labelledBy="deactivate-user-title">
          <div className="flex flex-col gap-4">
            <div>
              <h2 id="deactivate-user-title" className="text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
                Desactivar a {pendingDeactivation.user.nombre}
              </h2>
              <p className="mt-1 text-sm" style={{ color: 'var(--text-lo)' }}>
                Es juez de{' '}
                {pendingDeactivation.races.length === 1 ? 'esta carrera' : 'estas carreras'} en
                curso. La desactivación no se bloquea, pero revisá el impacto antes de confirmar.
              </p>
            </div>

            <ul className="flex flex-col gap-1 text-sm" style={{ color: 'var(--text-hi)' }}>
              {pendingDeactivation.races.map((race) => (
                <li key={race.id}>{race.nombre}</li>
              ))}
            </ul>

            <div className="flex justify-end gap-2">
              <Button variant="secondary" onClick={() => setPendingDeactivation(null)}>
                Cancelar
              </Button>
              <Button variant="destructive" onClick={handleConfirmDeactivation}>
                Desactivar de todos modos
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
