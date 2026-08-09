import { useMemo, useState } from 'react'
import type { CSSProperties } from 'react'
import { Button, Modal } from '@nicarunner/ui'
import type { DisputeEstado, ResolveDisputeRequest, TimingDisputeDto, TimingSource } from '../../api/types'
import { tableWrap } from '../../theme/styles'

const ESTADO_LABEL: Record<DisputeEstado, string> = {
  Oficial: 'Oficial',
  Revision: 'Revisión',
  Disputa: 'Disputa',
}

const ESTADO_STYLE: Record<DisputeEstado, CSSProperties> = {
  Oficial: {
    background: 'var(--ok-bg)',
    color: 'var(--ok-tx)',
    border: '1px solid var(--ok-bd)',
  },
  Revision: {
    background: 'var(--wn-bg)',
    color: 'var(--wn-tx)',
    border: '1px solid var(--wn-bd)',
  },
  Disputa: {
    background: 'var(--er-bg)',
    color: 'var(--er-tx)',
    border: '1px solid var(--er-bd)',
  },
}

const ROW_TINT: Record<DisputeEstado, string> = {
  Oficial: 'var(--row-ok)',
  Revision: 'var(--row-wn)',
  Disputa: 'var(--row-er)',
}

const SOURCE_LABEL: Record<TimingSource, string> = {
  Chip: 'Chip RFID',
  Checkpoint: 'Capturista (juez)',
  Camera: 'Cámara',
}

const ESTADOS_FILTRO: ('todos' | DisputeEstado)[] = ['todos', 'Oficial', 'Revision', 'Disputa']

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

function availableSources(row: TimingDisputeDto): TimingSource[] {
  const sources: TimingSource[] = []
  if (row.chipSeconds != null) sources.push('Chip')
  if (row.checkpointSeconds != null) sources.push('Checkpoint')
  if (row.cameraSeconds != null) sources.push('Camera')
  return sources
}

function sourceSeconds(row: TimingDisputeDto, source: TimingSource): number | null {
  if (source === 'Chip') return row.chipSeconds
  if (source === 'Checkpoint') return row.checkpointSeconds
  return row.cameraSeconds
}

export interface DisputeResolutionGridProps {
  rows: TimingDisputeDto[]
  canMutate: boolean
  busyId?: number | null
  onResolve: (id: number, request: ResolveDisputeRequest) => Promise<void>
}

export function DisputeResolutionGrid({
  rows,
  canMutate,
  busyId = null,
  onResolve,
}: DisputeResolutionGridProps) {
  const [search, setSearch] = useState('')
  const [filtro, setFiltro] = useState<'todos' | DisputeEstado>('todos')
  const [oficialRow, setOficialRow] = useState<TimingDisputeDto | null>(null)
  const [selectedSource, setSelectedSource] = useState<TimingSource | null>(null)

  const visibleRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    return rows.filter((row) => {
      const dorsal = row.dorsal ?? ''
      const matchesSearch =
        term === '' ||
        dorsal.toLowerCase().includes(term) ||
        row.corredorNombre.toLowerCase().includes(term)
      const matchesFiltro = filtro === 'todos' || row.estado === filtro
      return matchesSearch && matchesFiltro
    })
  }, [rows, search, filtro])

  function openOficial(row: TimingDisputeDto) {
    const sources = availableSources(row)
    setOficialRow(row)
    setSelectedSource(sources[0] ?? null)
  }

  async function confirmOficial() {
    if (!oficialRow || !selectedSource) return
    await onResolve(oficialRow.id, {
      estado: 'Oficial',
      selectedSource,
      confirmApplyOfficial: true,
    })
    setOficialRow(null)
    setSelectedSource(null)
  }

  async function markDisputa(row: TimingDisputeDto) {
    if (!confirm(`¿Marcar controversia de ${row.corredorNombre} (dorsal ${row.dorsal ?? '—'}) como Disputa?`)) {
      return
    }
    await onResolve(row.id, { estado: 'Disputa' })
  }

  return (
    <div className="nr-dispute-grid flex flex-col gap-2 font-sans">
      <div
        className="nr-dispute-toolbar sticky top-0 z-10 flex flex-wrap items-center gap-2 px-3 py-2"
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--bd)',
          borderRadius: 'var(--r-card)',
          minHeight: 44,
        }}
      >
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por dorsal o nombre…"
          aria-label="Buscar controversias"
          className="nr-input nr-dispute-search h-7 min-w-0 flex-1 px-2 text-sm sm:w-56 sm:flex-none"
        />
        <div className="nr-dispute-filters flex flex-wrap gap-1">
          {ESTADOS_FILTRO.map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => setFiltro(opt)}
              className="nr-dispute-chip h-7 px-2.5 text-xs font-medium"
              aria-pressed={filtro === opt}
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
      </div>

      <div className="table-scroll" style={{ ...tableWrap }}>
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
              const diff = diferencia(row.chipSeconds, row.checkpointSeconds)
              const isCritical = diff !== null && diff > 3
              const rowBusy = busyId === row.id
              return (
                <tr
                  key={row.id}
                  className="row-hover"
                  style={{
                    minHeight: 42,
                    borderBottom: '1px solid var(--bd-inner)',
                    background: ROW_TINT[row.estado],
                  }}
                >
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-hi)' }}>
                    {row.dorsal ?? '—'}
                  </td>
                  <td className="px-3" style={{ color: 'var(--tx-hi)' }}>
                    {row.corredorNombre}
                  </td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-md)' }}>
                    {formatTiempo(row.chipSeconds)}
                  </td>
                  <td className="px-3" style={{ verticalAlign: 'middle' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      <span
                        style={{
                          fontFamily: '"IBM Plex Mono", ui-monospace, monospace',
                          fontSize: 12,
                          fontWeight: 600,
                          color: 'var(--tx-hi)',
                        }}
                      >
                        {formatTiempo(row.checkpointSeconds)}
                      </span>
                      <span style={{ fontSize: 10.5, color: 'var(--tx-lo)' }}>
                        {row.capturistaNombre ?? '—'}
                        {row.capturaNote && (
                          <span
                            style={{
                              marginLeft: 4,
                              color: row.estado === 'Disputa' ? 'var(--er-tx)' : 'var(--wn-tx)',
                            }}
                          >
                            · {row.capturaNote}
                          </span>
                        )}
                      </span>
                    </div>
                  </td>
                  <td className="px-3 font-mono tabular-nums" style={{ color: 'var(--tx-md)' }}>
                    {formatTiempo(row.cameraSeconds)}
                  </td>
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
                    {canMutate ? (
                      <div className="nr-dispute-actions flex flex-wrap gap-1.5 py-1">
                        <button
                          type="button"
                          disabled={rowBusy || row.estado === 'Oficial'}
                          onClick={() => openOficial(row)}
                          className="nr-dispute-action h-6 px-2 text-xs font-medium disabled:opacity-50"
                          style={{
                            background: 'var(--ok-bg)',
                            border: '1px solid var(--ok-bd)',
                            color: 'var(--ok-tx)',
                            borderRadius: 'var(--r-btn)',
                          }}
                        >
                          Marcar oficial
                        </button>
                        <button
                          type="button"
                          disabled={rowBusy || row.estado === 'Disputa'}
                          onClick={() => void markDisputa(row)}
                          className="nr-dispute-action h-6 px-2 text-xs font-medium disabled:opacity-50"
                          style={{
                            background: 'var(--er-bg)',
                            border: '1px solid var(--er-bd)',
                            color: 'var(--er-tx)',
                            borderRadius: 'var(--r-btn)',
                          }}
                        >
                          Marcar disputa
                        </button>
                      </div>
                    ) : (
                      <span className="text-xs" style={{ color: 'var(--tx-lo)' }}>
                        Solo lectura
                      </span>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {oficialRow && (
        <Modal
          onClose={() => {
            setOficialRow(null)
            setSelectedSource(null)
          }}
          labelledBy="oficial-confirm-title"
        >
          <h2 id="oficial-confirm-title" className="mb-2 text-base font-semibold" style={{ color: 'var(--tx-hi)' }}>
            Confirmar tiempo oficial
          </h2>
          <p className="mb-3 text-sm" style={{ color: 'var(--tx-lo)' }}>
            {oficialRow.corredorNombre} (dorsal {oficialRow.dorsal ?? '—'}). Elegí la fuente del tiempo;
            el oficial no se aplica sin esta confirmación.
          </p>
          <fieldset className="mb-4 flex flex-col gap-2 border-0 p-0">
            <legend className="mb-1 text-xs font-medium" style={{ color: 'var(--tx-md)' }}>
              Fuente
            </legend>
            {availableSources(oficialRow).length === 0 && (
              <p className="text-sm" style={{ color: 'var(--er-tx)' }}>
                Ninguna fuente tiene tiempo disponible.
              </p>
            )}
            {availableSources(oficialRow).map((source) => (
              <label
                key={source}
                className="nr-dispute-source flex items-center gap-2 text-sm"
                style={{ color: 'var(--tx-hi)', minHeight: 32 }}
              >
                <input
                  type="radio"
                  name="selectedSource"
                  checked={selectedSource === source}
                  onChange={() => setSelectedSource(source)}
                />
                {SOURCE_LABEL[source]} — {formatTiempo(sourceSeconds(oficialRow, source))}
              </label>
            ))}
          </fieldset>
          <div className="flex justify-end gap-2">
            <Button
              onClick={() => {
                setOficialRow(null)
                setSelectedSource(null)
              }}
            >
              Cancelar
            </Button>
            <Button
              variant="primary"
              disabled={!selectedSource || busyId === oficialRow.id}
              onClick={() => void confirmOficial()}
            >
              {busyId === oficialRow.id ? 'Aplicando…' : 'Confirmar tiempo oficial'}
            </Button>
          </div>
        </Modal>
      )}

      {rows.length > 0 && visibleRows.length === 0 && (
        <p className="py-6 text-center text-sm" style={{ color: 'var(--tx-lo)' }}>
          Ninguna controversia coincide con la búsqueda o el filtro.
        </p>
      )}
    </div>
  )
}
