import { useState } from 'react'
import { Button, Input, Label } from '@nicarunner/ui'
import type { DisputeAssignment, DisputeGroupDto, DisputeMotivo } from '../../api/types'
import { card, cardTitle, textLo } from '../../theme/styles'

const MOTIVO_LABEL: Record<DisputeMotivo, string> = {
  DorsalDuplicado: 'Dorsal duplicado',
  CategoriaSinSalida: 'Categoría sin salida',
  CategoriaCerrada: 'Categoría cerrada',
}

function formatFecha(iso: string): string {
  return new Date(iso).toLocaleString('es-NI', { dateStyle: 'short', timeStyle: 'medium' })
}

export interface DisputeGroupCardProps {
  group: DisputeGroupDto
  busy: boolean
  onResolve: (asignaciones: DisputeAssignment[], anular: number[], razon: string) => Promise<void>
}

/**
 * Un grupo de resultados reclamando el mismo dorsal. Cada fila arranca con el dorsal
 * propuesto precargado — el admin decide, por fila, si asignarlo (editable) o anularla.
 * Al resolver, TODAS las filas se incluyen: las no anuladas van a `asignaciones` (aunque
 * el dorsal no haya cambiado, reafirmarlo limpia el estado de disputa en el servidor).
 */
export function DisputeGroupCard({ group, busy, onResolve }: DisputeGroupCardProps) {
  const [dorsalByResult, setDorsalByResult] = useState<Record<number, string>>(() =>
    Object.fromEntries(group.resultados.map((r) => [r.id, r.dorsalPropuesto ?? r.dorsal ?? ''])),
  )
  const [anuladas, setAnuladas] = useState<Set<number>>(new Set())
  const [razon, setRazon] = useState('')

  function toggleAnular(resultId: number) {
    setAnuladas((prev) => {
      const next = new Set(prev)
      if (next.has(resultId)) next.delete(resultId)
      else next.add(resultId)
      return next
    })
  }

  async function submit() {
    const asignaciones: DisputeAssignment[] = []
    const anular: number[] = []
    for (const r of group.resultados) {
      if (anuladas.has(r.id)) {
        anular.push(r.id)
      } else {
        asignaciones.push({ resultId: r.id, dorsal: dorsalByResult[r.id]?.trim() || null })
      }
    }
    await onResolve(asignaciones, anular, razon.trim())
  }

  const canSubmit = razon.trim().length > 0 && !busy

  return (
    <div style={{ ...card, display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-sm font-semibold" style={cardTitle}>
          Dorsal en disputa: {group.dorsalEnDisputa ?? '—'}
        </h2>
        <span className="text-xs" style={textLo}>
          {MOTIVO_LABEL[group.motivo]} · abierta {formatFecha(group.abiertaUtc)}
        </span>
      </div>

      <div className="flex flex-col gap-2">
        {group.resultados.map((r) => {
          const isAnulada = anuladas.has(r.id)
          return (
            <div
              key={r.id}
              className="flex flex-wrap items-center gap-3 px-3 py-2"
              style={{
                background: isAnulada ? 'var(--er-bg)' : 'var(--bg-input)',
                border: `1px solid ${isAnulada ? 'var(--er-bd)' : 'var(--bd)'}`,
                borderRadius: 'var(--radius-card)',
              }}
            >
              <div style={{ minWidth: 160 }}>
                <div className="text-sm font-medium" style={{ color: 'var(--text-hi)' }}>
                  {r.runnerNombre || '(sin corredor)'}
                </div>
                <div className="text-xs" style={textLo}>
                  Capturista {r.capturistaNombre} · {formatFecha(r.tiempoLlegada)}
                </div>
              </div>

              <div className="flex items-end gap-2">
                <div>
                  <Label htmlFor={`dorsal-${r.id}`}>Dorsal a asignar</Label>
                  <Input
                    id={`dorsal-${r.id}`}
                    value={dorsalByResult[r.id] ?? ''}
                    disabled={isAnulada || busy}
                    onChange={(e) =>
                      setDorsalByResult((prev) => ({ ...prev, [r.id]: e.target.value }))
                    }
                    style={{ width: 100 }}
                  />
                </div>
                <Button
                  type="button"
                  variant={isAnulada ? 'destructive' : 'secondary'}
                  size="sm"
                  disabled={busy}
                  onClick={() => toggleAnular(r.id)}
                >
                  {isAnulada ? 'Anulada' : 'Anular'}
                </Button>
              </div>
            </div>
          )
        })}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div style={{ flex: '1 1 240px' }}>
          <Label htmlFor={`razon-${group.disputeGroupId}`} required>Razón</Label>
          <Input
            id={`razon-${group.disputeGroupId}`}
            value={razon}
            disabled={busy}
            onChange={(e) => setRazon(e.target.value)}
            required
            placeholder="Motivo de la resolución"
            style={{ width: '100%' }}
          />
        </div>
        <Button
          type="button"
          variant="primary"
          disabled={!canSubmit}
          onClick={() => void submit()}
        >
          {busy ? 'Resolviendo…' : 'Resolver disputa'}
        </Button>
      </div>
    </div>
  )
}
