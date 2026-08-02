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
  capturista?: string
  capturaNote?: string
}

const CONNECTION_CYCLE: ConnectionState[] = ['online', 'syncing', 'offline']

const ESTADO_LABEL: Record<EstadoTiempo, string> = {
  oficial: 'Oficial',
  revision: 'Revisión',
  disputa: 'Disputa',
}

const ESTADO_STYLE: Record<EstadoTiempo, CSSProperties> = {
  oficial: {
    background: 'var(--ok-bg)',
    color: 'var(--ok-tx)',
    border: '1px solid var(--ok-bd)',
  },
  revision: {
    background: 'var(--wn-bg)',
    color: 'var(--wn-tx)',
    border: '1px solid var(--wn-bd)',
  },
  disputa: {
    background: 'var(--er-bg)',
    color: 'var(--er-tx)',
    border: '1px solid var(--er-bd)',
  },
}

const ROW_TINT: Record<EstadoTiempo, string> = {
  oficial:  'var(--row-ok)',
  revision: 'var(--row-wn)',
  disputa:  'var(--row-er)',
}

const MOCK_ROWS: DisputeRow[] = [
  { id: 1, dorsal: 118, corredor: 'Ana Pérez',     chipRfid: 2531.4, puntoControl: 2531.9, camara: 2531.6, estado: 'oficial',  capturista: 'M. López' },
  { id: 2, dorsal: 205, corredor: 'Luis Gómez',    chipRfid: 2559.1, puntoControl: 2565.8, camara: 2560.0, estado: 'revision', manualSync: 'pending', capturista: 'R. García', capturaNote: '2 capturas' },
  { id: 3, dorsal: 342, corredor: 'Karla Solís',   chipRfid: 2601.0, puntoControl: 2601.2, camara: null,   estado: 'disputa',  capturista: 'M. López' },
  { id: 4, dorsal: 87,  corredor: 'José Martínez', chipRfid: 2410.7, puntoControl: 2411.0, camara: 2410.9, estado: 'oficial',  capturista: 'R. García' },
  { id: 5, dorsal: 156, corredor: 'María Reyes',   chipRfid: null,   puntoControl: 2622.3, camara: 2621.8, estado: 'disputa',  manualSync: 'error', capturista: 'M. López', capturaNote: 'sin chip' },
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
          border: '1px solid var(--bd)',
          borderRadius: 'var(--r-card)',
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
                      background: 'var(--ac-bg)',
                      border: '1px solid var(--ac-dim)',
                      color: 'var(--ac)',
                      borderRadius: 'var(--r-btn)',
                    }
                  : {
                      background: 'var(--bg-input)',
                      border: '1px solid var(--bd)',
                      color: 'var(--tx-lo)',
                      borderRadius: 'var(--r-btn)',
                    }
              }
            >
              {opt === 'todos' ? 'Todos' : ESTADO_LABEL[opt]}
            </button>
          ))}
        </div>
        <span className="ml-auto text-xs" style={{ color: 'var(--tx-lo)' }}>
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
                color: 'var(--tx-th)',
                borderBottom: '1px solid var(--bd)',
              }}
            >
              <th className="h-8 px-3 font-medium">Dorsal</th>
              <th className="h-8 px-3 font-medium">Corredor</th>
              <th className="h-8 px-3 font-medium">Chip RFID</th>
              <th className="h-8 px-3 font-medium">Capturista (juez)</th>
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
              return (
                <tr
                  key={row.id}
                  className="row-hover"
                  style={{
                    height: 42,
                    borderBottom: '1px solid var(--bd-inner)',
                    background: ROW_TINT[row.estado],
                  }}
                >
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-hi)' }}>{row.dorsal}</td>
                  <td className="px-3" style={{ color: 'var(--tx-hi)' }}>{row.corredor}</td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-md)' }}>{formatTiempo(row.chipRfid)}</td>
                  <td className="px-3" style={{ verticalAlign: 'middle' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      <span
                        style={{
                          fontFamily: '"IBM Plex Mono", ui-monospace, monospace',
                          fontSize: 12,
                          fontWeight: 600,
                          color: 'var(--tx-hi)',
                          display: 'flex',
                          alignItems: 'center',
                          gap: 4,
                        }}
                      >
                        {row.manualSync && (
                          <span
                            title={row.manualSync === 'pending' ? 'Pendiente de sincronizar' : 'Error de sincronización'}
                            className={row.manualSync === 'pending' ? 'motion-safe:animate-pulse' : ''}
                            style={{
                              width: 6,
                              height: 6,
                              borderRadius: '50%',
                              flexShrink: 0,
                              display: 'inline-block',
                              background: row.manualSync === 'pending' ? 'var(--ac)' : 'var(--er-bd)',
                            }}
                          />
                        )}
                        {formatTiempo(row.puntoControl)}
                      </span>
                      <span style={{ fontSize: 10.5, color: 'var(--tx-lo)' }}>
                        {row.capturista ?? '—'}
                        {(row.capturaNote ?? (row.manualSync === 'pending' ? 'sincronizando…' : row.manualSync === 'error' ? 'error sync' : undefined)) && (
                          <span
                            style={{
                              marginLeft: 4,
                              color: row.estado === 'disputa' ? 'var(--er-tx)' : 'var(--wn-tx)',
                            }}
                          >
                            · {row.capturaNote ?? (row.manualSync === 'pending' ? 'sincronizando…' : 'error sync')}
                          </span>
                        )}
                      </span>
                    </div>
                  </td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-md)' }}>{formatTiempo(row.camara)}</td>
                  <td
                    className="px-3 font-mono tabular-nums"
                    style={{
                      color: isCritical ? 'var(--er-tx)' : 'var(--tx-lo)',
                      fontWeight: isCritical ? 600 : 400,
                    }}
                  >
                    {diff === null ? '—' : `${diff.toFixed(1)}s`}
                  </td>
                  <td className="px-3">
                    <span
                      className="px-2 py-0.5 text-xs font-medium"
                      style={{ ...ESTADO_STYLE[row.estado], borderRadius: 'var(--r-badge)' }}
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
                          background: 'var(--ok-bg)',
                          border: '1px solid var(--ok-bd)',
                          color: 'var(--ok-tx)',
                          borderRadius: 'var(--r-btn)',
                        }}
                      >
                        Oficial
                      </button>
                      <button
                        type="button"
                        onClick={() => setEstado(row.id, 'disputa')}
                        className="h-6 px-2 text-xs font-medium"
                        style={{
                          background: 'var(--er-bg)',
                          border: '1px solid var(--er-bd)',
                          color: 'var(--er-tx)',
                          borderRadius: 'var(--r-btn)',
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
