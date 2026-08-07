import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { deleteRace, getRaceAudit, getRaces } from '../../api/endpoints'
import type { RaceDto, RaceStatus } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, Toolbar, Pagination, Badge, SectionHeader } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { Select } from '@nicarunner/ui'
import { RaceFormModal } from './RaceFormModal'
import { EntityAuditHistory } from '../../components/EntityAuditHistory'

const PAGE_SIZE = 20

const STATUS_VARIANT: Record<RaceStatus, 'success' | 'neutral'> = {
  EnCurso: 'success',
  Terminada: 'success',
  Planeada: 'neutral',
}

const STATUS_LABEL: Record<RaceStatus, string> = {
  Planeada: 'Planeada',
  EnCurso: 'En curso',
  Terminada: 'Finalizada',
}

function raceStatusBadge(status: RaceStatus) {
  return (
    <Badge variant={STATUS_VARIANT[status]} live={status === 'EnCurso'}>
      {STATUS_LABEL[status]}
    </Badge>
  )
}

export function RacesPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<RaceDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [auditingRace, setAuditingRace] = useState<RaceDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [year, setYear] = useState('')
  const [page, setPage] = useState(1)

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
    setError(null)
    try {
      await deleteRace(race.id)
      reload()
    } catch (err: any) {
      setError(err.response?.data?.detail ?? 'No se pudo eliminar la carrera.')
    }
  }

  const years = useMemo(
    () => [...new Set(races.map((r) => new Date(r.fechaCarrera).getFullYear()))].sort().reverse(),
    [races],
  )

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return races.filter((r) => {
      if (year && new Date(r.fechaCarrera).getFullYear() !== Number(year)) return false
      if (!q) return true
      return r.nombre.toLowerCase().includes(q)
    })
  }, [races, search, year])

  const pageItems = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE
    return filtered.slice(start, start + PAGE_SIZE)
  }, [filtered, page])

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
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Estado',
      render: (race) => raceStatusBadge(race.estado),
    },
    {
      header: '',
      render: (race) => (
        <div className="flex gap-2">
          <Link to={`/carreras/${race.id}?tab=corredores`}>
            <Button size="sm">Corredores</Button>
          </Link>
          {canManage && (
            <>
              <Button size="sm" variant="ghost" onClick={() => setEditing(race)}>
                Editar
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setAuditingRace(race)}>
                Historial
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
      <SectionHeader
        title="Todas las carreras"
        subtitle={`${filtered.length} carreras registradas`}
        actions={
          canManage && (
            <Button variant="primary" onClick={() => setShowCreate(true)}>
              + Nueva carrera
            </Button>
          )
        }
      />

      <Toolbar
        searchPlaceholder="Buscar carrera..."
        onSearch={(value) => {
          setSearch(value)
          setPage(1)
        }}
      >
        <Select
          value={year}
          onChange={(e) => {
            setYear(e.target.value)
            setPage(1)
          }}
          className="w-auto"
          aria-label="Filtrar por año"
        >
          <option value="">Todas las fechas</option>
          {years.map((y) => (
            <option key={y} value={y}>
              {y}
            </option>
          ))}
        </Select>
      </Toolbar>

      {error && <p className="text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}

      {loading && <LoadingText message="Cargando carreras..." />}

      {!loading && (
        <>
          <DataTable
            columns={columns}
            data={pageItems}
            rowKey={(race) => race.id}
            emptyState={<EmptyState message="No hay carreras creadas todavía." />}
          />
          {filtered.length > 0 && (
            <Pagination page={page} pageSize={PAGE_SIZE} total={filtered.length} onChange={setPage} />
          )}
        </>
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

      {auditingRace && (
        <EntityAuditHistory
          title={`Auditoría — carrera "${auditingRace.nombre}"`}
          load={() => getRaceAudit(auditingRace.id)}
          onClose={() => setAuditingRace(null)}
        />
      )}
    </div>
  )
}