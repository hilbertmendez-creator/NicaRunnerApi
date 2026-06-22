import { useMemo, useState } from 'react'
import { ConnectionStatusBadge, type ConnectionState } from '../../components/ConnectionStatusBadge'

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

const ESTADO_CLASSES: Record<EstadoTiempo, string> = {
  oficial: 'bg-official-50 text-official-600 border-official-200',
  revision: 'bg-dispute-50 text-dispute-600 border-dispute-200',
  disputa: 'bg-critical-50 text-critical-600 border-critical-200',
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
      <div className="sticky top-0 z-10 flex h-11 items-center gap-3 border border-zinc-200 bg-white px-3">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por dorsal o nombre…"
          className="h-7 w-56 border border-zinc-200 bg-zinc-50 px-2 text-sm text-zinc-900 outline-none focus:border-blue-700 focus:ring-1 focus:ring-blue-700"
        />
        <div className="flex gap-1">
          {ESTADOS_FILTRO.map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => setFiltro(opt)}
              className={`h-7 border px-2.5 text-xs font-medium ${
                filtro === opt
                  ? 'border-blue-700 bg-blue-700 text-white'
                  : 'border-zinc-200 bg-white text-zinc-600 hover:bg-zinc-50'
              }`}
            >
              {opt === 'todos' ? 'Todos' : ESTADO_LABEL[opt]}
            </button>
          ))}
        </div>
        <span className="ml-auto text-xs text-zinc-400">{visibleRows.length} resultados</span>
        <span className="h-4 w-px bg-zinc-200" />
        <ConnectionStatusBadge state={connection} onClick={cycleConnection} />
      </div>

      <div className="overflow-x-auto border border-zinc-200 bg-white">
        <table className="w-full border-collapse text-left text-sm">
          <thead>
            <tr className="border-b border-zinc-200 bg-zinc-50 text-xs uppercase tracking-wide text-zinc-500">
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
              const syncBorder =
                row.manualSync === 'pending'
                  ? 'border-l-blue-700'
                  : row.manualSync === 'error'
                    ? 'border-l-critical-600'
                    : 'border-l-transparent'
              return (
                <tr key={row.id} className={`h-9 border-b border-l-2 border-zinc-100 hover:bg-zinc-50 ${syncBorder}`}>
                  <td className="px-3 font-mono tabular-nums text-zinc-900">{row.dorsal}</td>
                  <td className="px-3 text-zinc-900">{row.corredor}</td>
                  <td className="px-3 font-mono tabular-nums text-zinc-700">{formatTiempo(row.chipRfid)}</td>
                  <td className="px-3 font-mono tabular-nums text-zinc-700">
                    <span className="inline-flex items-center gap-1.5">
                      {row.manualSync && (
                        <span
                          title={
                            row.manualSync === 'pending'
                              ? 'Edición manual pendiente de sincronizar'
                              : 'Error al sincronizar — reintentando'
                          }
                          className={`h-1.5 w-1.5 rounded-full ${
                            row.manualSync === 'pending'
                              ? 'bg-blue-700 motion-safe:animate-pulse'
                              : 'bg-critical-600'
                          }`}
                        />
                      )}
                      {formatTiempo(row.puntoControl)}
                    </span>
                  </td>
                  <td className="px-3 font-mono tabular-nums text-zinc-700">{formatTiempo(row.camara)}</td>
                  <td
                    className={`px-3 font-mono tabular-nums ${isCritical ? 'font-semibold text-critical-600' : 'text-zinc-500'}`}
                  >
                    {diff === null ? '—' : `${diff.toFixed(1)}s`}
                  </td>
                  <td className="px-3">
                    <span className={`border px-2 py-0.5 text-xs font-medium ${ESTADO_CLASSES[row.estado]}`}>
                      {ESTADO_LABEL[row.estado]}
                    </span>
                  </td>
                  <td className="px-3">
                    <div className="flex gap-1.5">
                      <button
                        type="button"
                        onClick={() => setEstado(row.id, 'oficial')}
                        className="h-6 border border-official-200 bg-official-50 px-2 text-xs font-medium text-official-600 hover:border-official-600"
                      >
                        Oficial
                      </button>
                      <button
                        type="button"
                        onClick={() => setEstado(row.id, 'disputa')}
                        className="h-6 border border-critical-200 bg-critical-50 px-2 text-xs font-medium text-critical-600 hover:border-critical-600"
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
