import { useEffect, useMemo, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { useRace } from '../../hooks/useRace'
import { getResults, notifyAll, notifyResult } from '../../api/endpoints'
import type { ResultDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import {
  Button,
  DataTable,
  LoadingText,
  EmptyState,
  Toolbar,
  PosCell,
  Pagination,
  SectionHeader,
} from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { Select } from '@nicarunner/ui'
import { EditResultModal } from './EditResultModal'
import { AuditHistory } from './AuditHistory'

const PAGE_SIZE = 20

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

export function ResultsPage() {
  const { user } = useAuth()
  const canEdit = user?.role === 'Administrador'

  const { raceId } = useRace()
  const [results, setResults] = useState<ResultDto[]>([])
  const [loading, setLoading] = useState(false)
  const [editing, setEditing] = useState<ResultDto | null>(null)
  const [auditingId, setAuditingId] = useState<number | null>(null)
  const [notifyingId, setNotifyingId] = useState<number | null>(null)
  const [notifyingAll, setNotifyingAll] = useState(false)
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [page, setPage] = useState(1)

  async function handleNotify(resultId: number) {
    setNotifyingId(resultId)
    try {
      await notifyResult(resultId)
    } finally {
      setNotifyingId(null)
    }
  }

  async function handleNotifyAll() {
    if (!raceId) return
    if (!confirm('¿Enviar notificaciones a todos los corredores con tiempo registrado?')) return
    setNotifyingAll(true)
    try {
      await notifyAll(raceId)
    } finally {
      setNotifyingAll(false)
    }
  }

  function reload() {
    if (!raceId) return
    setLoading(true)
    getResults(raceId)
      .then(setResults)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  const categories = useMemo(
    () => [...new Set(results.map((r) => r.categoriaNombre).filter(Boolean))].sort(),
    [results],
  )

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return results.filter((r) => {
      if (category && r.categoriaNombre !== category) return false
      if (!q) return true
      return (
        r.dorsal?.toLowerCase().includes(q) ||
        r.runnerNombre.toLowerCase().includes(q) ||
        formatTime(r.tiempoLlegada).toLowerCase().includes(q)
      )
    })
  }, [results, search, category])

  const pageItems = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE
    return filtered.slice(start, start + PAGE_SIZE)
  }, [filtered, page])

  const columns: Column<ResultDto>[] = [
    {
      header: 'Pos',
      render: (result) => <PosCell rank={result.posicion} />,
    },
    {
      header: 'Dorsal',
      render: (result) => result.dorsal ?? '—',
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Nombre',
      render: (result) => result.runnerNombre,
    },
    {
      header: 'Categoría',
      render: (result) => result.categoriaNombre ?? '—',
    },
    {
      header: 'Tiempo oficial',
      render: (result) => formatTime(result.tiempoLlegada),
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Última edición',
      render: (result) => new Date(result.updatedAt).toLocaleString('es-NI'),
      className: 'font-mono tabular-nums',
    },
    {
      header: '',
      render: (result) => (
        <div className="flex gap-2">
          <Button size="sm" onClick={() => setAuditingId(result.id)}>
            Auditoría
          </Button>
          {canEdit && (
            <>
              <Button size="sm" variant="ghost" onClick={() => setEditing(result)}>
                Editar
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => handleNotify(result.id)}
                disabled={notifyingId === result.id}
              >
                {notifyingId === result.id ? 'Enviando...' : 'Notificar'}
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
        title="Resultados oficiales"
        subtitle={`${results.length} tiempos registrados`}
        actions={
          <>
            <RaceSelector />
            {canEdit && (
              <Button variant="info" onClick={handleNotifyAll} disabled={!raceId || notifyingAll}>
                {notifyingAll ? 'Enviando...' : 'Notificar a todos'}
              </Button>
            )}
          </>
        }
      />

      <Toolbar
        searchPlaceholder="Buscar por dorsal, nombre o tiempo..."
        onSearch={(value) => {
          setSearch(value)
          setPage(1)
        }}
      >
        <Select
          value={category}
          onChange={(e) => {
            setCategory(e.target.value)
            setPage(1)
          }}
          className="w-auto"
          aria-label="Filtrar por categoría"
        >
          <option value="">Todas las categorías</option>
          {categories.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </Select>
      </Toolbar>

      {loading && <LoadingText message="Cargando resultados..." />}

      {!loading && raceId && (
        <>
          <DataTable
            columns={columns}
            data={pageItems}
            rowKey={(result) => result.id}
            emptyState={<EmptyState message="Esta carrera no tiene resultados capturados todavía." />}
          />
          {filtered.length > 0 && (
            <Pagination page={page} pageSize={PAGE_SIZE} total={filtered.length} onChange={setPage} />
          )}
        </>
      )}

      {editing && raceId && (
        <EditResultModal
          raceId={raceId}
          result={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}

      {auditingId && raceId && (
        <AuditHistory raceId={raceId} resultId={auditingId} onClose={() => setAuditingId(null)} />
      )}
    </div>
  )
}
