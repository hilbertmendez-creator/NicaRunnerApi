import { useEffect, useMemo, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { useRace } from '../../hooks/useRace'
import { getControversies, resolveControversy } from '../../api/endpoints'
import type { ControversyDto } from '../../api/types'
import {
  Badge,
  Button,
  DataTable,
  EmptyState,
  ErrorAlert,
  LoadingText,
  SectionHeader,
  Toolbar,
} from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'

function formatTiempo(seconds: number | null) {
  if (seconds === null) return '—'
  const m = Math.floor(seconds / 60)
  const s = (seconds % 60).toFixed(1).padStart(4, '0')
  return `${String(m).padStart(2, '0')}:${s}`
}

function diferencia(row: ControversyDto): number | null {
  if (row.diferencia !== null) return row.diferencia
  if (row.tiempoChip !== null && row.tiempoCaptura !== null) return row.tiempoChip - row.tiempoCaptura
  return null
}

export function ControversiasPage() {
  const { raceId } = useRace()
  const [rows, setRows] = useState<ControversyDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [resolvingId, setResolvingId] = useState<number | null>(null)

  function reload() {
    if (!raceId) return
    setLoading(true)
    setError(null)
    getControversies(raceId)
      .then(setRows)
      .catch(() => setError('No se pudieron cargar las controversias. Intenta de nuevo.'))
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  const abiertas = rows.filter((row) => row.estado === 'Abierta').length
  const resueltas = rows.length - abiertas

  const visibleRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return rows
    return rows.filter(
      (row) =>
        row.dorsal.toLowerCase().includes(term) || row.nombre.toLowerCase().includes(term),
    )
  }, [rows, search])

  async function handleResolve(row: ControversyDto) {
    if (!raceId) return
    if (!confirm(`¿Resolver la controversia del dorsal ${row.dorsal}?`)) return
    setResolvingId(row.id)
    try {
      await resolveControversy(raceId, row.id, { estado: 'Resuelta' })
      reload()
    } catch {
      setError('No se pudo resolver la controversia. Intenta de nuevo.')
    } finally {
      setResolvingId(null)
    }
  }

  const columns: Column<ControversyDto>[] = [
    { header: 'Dorsal', render: (row) => row.dorsal, className: 'font-mono tabular-nums' },
    { header: 'Corredor', render: (row) => row.nombre },
    { header: 'Categoría', render: (row) => row.categoria },
    { header: 'Chip', render: (row) => formatTiempo(row.tiempoChip), className: 'font-mono tabular-nums' },
    { header: 'Captura', render: (row) => formatTiempo(row.tiempoCaptura), className: 'font-mono tabular-nums' },
    { header: 'Cámara', render: (row) => formatTiempo(row.tiempoCamara), className: 'font-mono tabular-nums' },
    {
      header: 'Dif.',
      render: (row) => {
        const diff = diferencia(row)
        return diff === null ? '—' : `${diff.toFixed(1)}s`
      },
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Estado',
      render: (row) =>
        row.estado === 'Abierta' ? (
          <Badge variant="warning">Abierta</Badge>
        ) : (
          <Badge variant="success">Resuelta</Badge>
        ),
    },
    {
      header: '',
      render: (row) =>
        row.estado === 'Abierta' && (
          <Button
            size="sm"
            variant="info"
            onClick={() => handleResolve(row)}
            disabled={resolvingId === row.id}
          >
            {resolvingId === row.id ? 'Resolviendo...' : 'Resolver'}
          </Button>
        ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <SectionHeader
        title="Controversias"
        subtitle={`${abiertas} controversias abiertas · ${resueltas} resueltas`}
        actions={<RaceSelector />}
      />

      <Toolbar searchPlaceholder="Buscar por dorsal o nombre…" onSearch={setSearch} />

      {loading && <LoadingText message="Cargando controversias..." />}

      {error && (
        <div className="flex items-center gap-3">
          <ErrorAlert message={error} className="flex-1" />
          <Button variant="secondary" size="sm" onClick={reload}>
            Reintentar
          </Button>
        </div>
      )}

      {!loading && !error && raceId && (
        <DataTable
          columns={columns}
          data={visibleRows}
          rowKey={(row) => row.id}
          emptyState={<EmptyState message="No hay disputas de tiempo." />}
        />
      )}
    </div>
  )
}