import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { deleteRace, getRaces } from '../../api/endpoints'
import type { RaceDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { StatusBadge } from '../../components/StatusBadge'
import { Button, DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { RaceFormModal } from './RaceFormModal'
import { pageTitle } from '../../theme/styles'

export function RacesPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<RaceDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  function reload() {
    setLoading(true)
    getRaces()
      .then(setRaces)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [])

  async function handleDelete(race: RaceDto) {
    if (!confirm(`¿Eliminar la carrera "${race.nombre}"? Esta acción no se puede deshacer.`)) return
    await deleteRace(race.id)
    reload()
  }

  const columns: Column<RaceDto>[] = [
    {
      header: 'Nombre',
      render: (race) => (
        <Link
          to={`/carreras/${race.id}`}
          className="font-medium hover:underline"
          style={{ color: 'var(--accent)' }}
        >
          {race.nombre}
        </Link>
      ),
    },
    {
      header: 'Fecha',
      render: (race) => new Date(race.fechaCarrera).toLocaleDateString('es-NI'),
    },
    {
      header: 'Estado',
      render: (race) => <StatusBadge status={race.estado} />,
    },
    {
      header: '',
      render: (race) => (
        <div className="flex gap-2">
          {canManage && (
            <>
              <Button size="sm" onClick={() => setEditing(race)}>
                Editar
              </Button>
              <Button size="sm" variant="destructive" onClick={() => handleDelete(race)}>
                Eliminar
              </Button>
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold" style={pageTitle}>Carreras</h1>
        {canManage && (
          <Button variant="primary" onClick={() => setShowCreate(true)}>
            Nueva carrera
          </Button>
        )}
      </div>

      {loading && <LoadingText message="Cargando carreras..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={races}
          rowKey={(race) => race.id}
          emptyState={<EmptyState message="No hay carreras creadas todavía." />}
        />
      )}

      {showCreate && (
        <RaceFormModal
          race={null}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <RaceFormModal
          race={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}
    </div>
  )
}
