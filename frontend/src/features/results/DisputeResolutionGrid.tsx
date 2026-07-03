import { useMemo, useState } from 'react'
import type { CSSProperties } from 'react'
import { ConnectionStatusBadge, type ConnectionState } from '../../components/ConnectionStatusBadge'
import { tableWrap } from '../../theme/styles'

type EstadoTiempo = 'oficial' | 'revision' | 'disputa'
type ManualSyncState = 'pending' | 'error'

interface DisputeRow {
  id: number
  dorsal: number
  corredor: string
  chipRfid: number | null
  puntoControl: number | null
  camara: number | null
  estado: EstadoTiempo
  /** Punto de control ingresado manualmente y aún no confirmado por el servidor. Ausente = sincronizado. */
  manualSync?: ManualSyncState
}

const CONNECTION_CYCLE: ConnectionState[] = ['online', 'syncing', 'offline']

const ESTADO_LABEL: Record<EstadoTiempo, string> = {
  oficial: 'Oficial',
  revision: 'Revisión',
  disputa: 'Disputa',
}

const ESTADO_STYLE: Record<EstadoTiempo, CSSProperties> = {
  oficial: {
    background: 'var(--badge-ok-bg)',
    color: 'var(--badge-ok-text)',
    border: '1px solid var(--badge-ok-bd)',
  },
  revision: {
    background: 'var(--badge-pr-bg)',
    color: 'var(--badge-pr-text)',
    border: '1px solid var(--badge-pr-bd)',
  },
  disputa: {
    background: 'var(--badge-er-bg)',
    color: 'var(--badge-er-text)',
    border: '1px solid var(--badge-er-bd)',
  },
}

const MOCK_ROWS: DisputeRow[] = [
  { id: 1, dorsal: 118, corredor: 'Ana Pérez', chipRfid: 2531.4, puntoControl: 2531.9, camara: 2531.6, estado: 'oficial' },
  { id: 2, dorsal: 205, corredor: 'Luis Gómez', chipRfid: 2559.1, puntoControl: 2565.8, camara: 2560.0, estado: 'revision', manualSync: 'pending' },
  { id: 3, dorsal: 342, corredor: 'Karla Solís', chipRfid: 2601.0, puntoControl: 2601.2, camara: null, estado: 'disputa' },
  { id: 4, dorsal: 87, corredor: 'José Martínez', chipRfid: 2410.7, puntoControl: 2411.0, camara: 2410.9, estado: 'oficial' },
  { id: 5, dorsal: 156, corredor: 'María Reyes', chipRfid: null, puntoControl: 2622.3, camara: 2621.8, estado: 'disputa', manualSync: 'error' },
]

function formatTiempo(seconds: number | null) {
  if (seconds === null) return '—'
  const m = Math.floor(seconds / 60)
  const s = (seconds % 60).toFixed(1).padStart(4, '0')
  return `${String(m).padStart(2, '0')}:${s}`
}

function diferencia(chip: number | null, checkpoint: number | null) {
  if (chip === null || checkpoint === null) return null
  return Math.abs(chip - checkpoint)
}

const ESTADOS_FILTRO: ('todos' | EstadoTiempo)[] = ['todos', 'oficial', 'revision', 'disputa']

export function DisputeResolutionGrid() {
  const [rows, setRows] = useState(MOCK_ROWS)
  const [search, setSearch] = useState('')
  const [filtro, setFiltro] = useState<'todos' | EstadoTiempo>('todos')
  const [connection, setConnection] = useState<ConnectionState>('online')

  function cycleConnection() {
    setConnection((prev) => CONNECTION_CYCLE[(CONNECTION_CYCLE.indexOf(prev) + 1) % CONNECTION_CYCLE.length])
  }

  const visibleRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    return rows.filter((row) => {
      const matchesSearch =
        term === '' || String(row.dorsal).includes(term) || row.corredor.toLowerCase().includes(term)
      const matchesFiltro = filtro === 'todos' || row.estado === filtro
      return matchesSearch && matchesFiltro
    })
  }, [rows, search, filtro])

  function setEstado(id: number, estado: EstadoTiempo) {
    setRows((prev) => prev.map((row) => (row.id === id ? { ...row, estado } : row)))
  }

  return (
    <div className="flex flex-col gap-2 font-sans">
      <div
        className="sticky top-0 z-10 flex h-11 items-center gap-3 px-3"
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--bd-card)',
          borderRadius: 'var(--radius-card)',
        }}
      >
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por dorsal o nombre…"
          className="nr-input h-7 w-56 px-2 text-sm"
        />
        <div className="flex gap-1">
          {ESTADOS_FILTRO.map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => setFiltro(opt)}
              className="h-7 px-2.5 text-xs font-medium"
              style={
                filtro === opt
                  ? {
                      background: 'var(--chip-a-bg)',
                      border: '1px solid var(--chip-a-bd)',
                      color: 'var(--chip-a-text)',
                      borderRadius: 'var(--radius-btn)',
                    }
                  : {
                      background: 'var(--bg-input)',
                      border: '1px solid var(--bd)',
                      color: 'var(--text-xs)',
                      borderRadius: 'var(--radius-btn)',
                    }
              }
            >
              {opt === 'todos' ? 'Todos' : ESTADO_LABEL[opt]}
            </button>
          ))}
        </div>
        <span className="ml-auto text-xs" style={{ color: 'var(--text-xs)' }}>
          {visibleRows.length} resultados
        </span>
        <span className="h-4 w-px" style={{ background: 'var(--bd)' }} />
        <ConnectionStatusBadge state={connection} onClick={cycleConnection} />
      </div>

      <div style={{ ...tableWrap }}>
        <table className="w-full border-collapse text-left text-sm">
          <thead>
            <tr
              className="text-xs uppercase tracking-wide"
              style={{
                background: 'var(--bg-th)',
                color: 'var(--text-th)',
                borderBottom: '1px solid var(--bd)',
              }}
            >
              <th className="h-8 px-3 font-medium">Dorsal</th>
              <th className="h-8 px-3 font-medium">Corredor</th>
              <th className="h-8 px-3 font-medium">Chip RFID</th>
              <th className="h-8 px-3 font-medium">Punto control</th>
              <th className="h-8 px-3 font-medium">Cámara</th>
              <th className="h-8 px-3 font-medium">Diferencia</th>
              <th className="h-8 px-3 font-medium">Estado</th>
              <th className="h-8 px-3 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {visibleRows.map((row) => {
              const diff = diferencia(row.chipRfid, row.puntoControl)
              const isCritical = diff !== null && diff > 3
              const syncBorderColor =
                row.manualSync === 'pending'
                  ? 'var(--accent)'
                  : row.manualSync === 'error'
                    ? 'var(--conflict-bd)'
                    : 'transparent'
              return (
                <tr
                  key={row.id}
                  className="row-hover h-9"
                  style={{
                    borderBottom: '1px solid var(--bd-row)',
                    borderLeft: `2px solid ${syncBorderColor}`,
                  }}
                >
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--text-hi)' }}>{row.dorsal}</td>
                  <td className="px-3" style={{ color: 'var(--text-hi)' }}>{row.corredor}</td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--text-lo)' }}>{formatTiempo(row.chipRfid)}</td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--text-lo)' }}>
                    <span className="inline-flex items-center gap-1.5">
                      {row.manualSync && (
                        <span
                          title={
                            row.manualSync === 'pending'
                              ? 'Edición manual pendiente de sincronizar'
                              : 'Error al sincronizar — reintentando'
                          }
                          className={`h-1.5 w-1.5 rounded-full ${row.manualSync === 'pending' ? 'motion-safe:animate-pulse' : ''}`}
                          style={{
                            background:
                              row.manualSync === 'pending' ? 'var(--accent)' : 'var(--conflict-bd)',
                          }}
                        />
                      )}
                      {formatTiempo(row.puntoControl)}
                    </span>
                  </td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--text-lo)' }}>{formatTiempo(row.camara)}</td>
                  <td
                    className="px-3 font-mono tabular-nums"
                    style={{
                      color: isCritical ? 'var(--conflict-text)' : 'var(--text-xs)',
                      fontWeight: isCritical ? 600 : 400,
                    }}
                  >
                    {diff === null ? '—' : `${diff.toFixed(1)}s`}
                  </td>
                  <td className="px-3">
                    <span
                      className="px-2 py-0.5 text-xs font-medium"
                      style={{ ...ESTADO_STYLE[row.estado], borderRadius: 'var(--radius-badge)' }}
                    >
                      {ESTADO_LABEL[row.estado]}
                    </span>
                  </td>
                  <td className="px-3">
                    <div className="flex gap-1.5">
                      <button
                        type="button"
                        onClick={() => setEstado(row.id, 'oficial')}
                        className="h-6 px-2 text-xs font-medium"
                        style={{
                          background: 'var(--badge-ok-bg)',
                          border: '1px solid var(--badge-ok-bd)',
                          color: 'var(--badge-ok-text)',
                          borderRadius: 'var(--radius-btn)',
                        }}
                      >
                        Oficial
                      </button>
                      <button
                        type="button"
                        onClick={() => setEstado(row.id, 'disputa')}
                        className="h-6 px-2 text-xs font-medium"
                        style={{
                          background: 'var(--badge-er-bg)',
                          border: '1px solid var(--badge-er-bd)',
                          color: 'var(--badge-er-text)',
                          borderRadius: 'var(--radius-btn)',
                        }}
                      >
                        Disputa
                      </button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
