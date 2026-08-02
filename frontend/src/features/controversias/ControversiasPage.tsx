import { useEffect, useState } from 'react'
import { getControversias, resolveControversia } from '../../api/endpoints'
import type { ResolveDisputeRequest, TimingDisputeDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { useActiveRace } from '../../hooks/useActiveRace'
import { EmptyState, LoadingText } from '@nicarunner/ui'
import { pageTitle, textLo } from '../../theme/styles'
import { DisputeResolutionGrid } from '../results/DisputeResolutionGrid'

/** Notifica al shell (badge open-count) tras mutar controversias. */
export const CONTROVERSIAS_CHANGED_EVENT = 'nr:controversias-changed'

export function ControversiasPage() {
  const { user } = useAuth()
  const canMutate = user?.role === 'Administrador'
  const { raceId } = useActiveRace()

  const [rows, setRows] = useState<TimingDisputeDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)

  function reload() {
    if (!raceId) {
      setRows([])
      return
    }
    setLoading(true)
    setError(null)
    getControversias(raceId)
      .then(setRows)
      .catch(() => {
        setRows([])
        setError('No se pudieron cargar las controversias.')
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  async function handleResolve(id: number, request: ResolveDisputeRequest) {
    if (!raceId) return
    setBusyId(id)
    setError(null)
    try {
      const updated = await resolveControversia(raceId, id, request)
      setRows((prev) => prev.map((row) => (row.id === id ? updated : row)))
      window.dispatchEvent(new Event(CONTROVERSIAS_CHANGED_EVENT))
    } catch {
      setError('No se pudo resolver la controversia. Revisa confirmación y fuente.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-lg font-semibold" style={pageTitle}>
          Controversias
        </h1>
        <p className="mt-1 text-sm" style={textLo}>
          Resolución de tiempos multi-fuente para la carrera activa.
        </p>
      </div>

      {error && (
        <p className="text-sm" role="alert" style={{ color: 'var(--er-tx)' }}>
          {error}
        </p>
      )}

      {!raceId && !loading && (
        <EmptyState message="Selecciona una carrera en la barra superior para ver controversias." />
      )}

      {loading && <LoadingText message="Cargando controversias..." />}

      {!loading && raceId && rows.length === 0 && (
        <EmptyState message="No hay controversias abiertas para esta carrera. Usa los filtros cuando existan casos." />
      )}

      {!loading && raceId && rows.length > 0 && (
        <DisputeResolutionGrid
          rows={rows}
          canMutate={canMutate}
          busyId={busyId}
          onResolve={handleResolve}
        />
      )}
    </div>
  )
}
