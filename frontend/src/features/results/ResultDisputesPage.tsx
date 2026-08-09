import { useEffect, useState } from 'react'
import { getResultDisputes, resolveResultDispute } from '../../api/endpoints'
import type { DisputeAssignment, DisputeGroupDto } from '../../api/types'
import { useActiveRace } from '../../hooks/useActiveRace'
import { EmptyState, LoadingText } from '@nicarunner/ui'
import { pageTitle, textLo } from '../../theme/styles'
import { DisputeGroupCard } from './DisputeGroupCard'

export function ResultDisputesPage() {
  const { raceId } = useActiveRace()

  const [groups, setGroups] = useState<DisputeGroupDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busyGroupId, setBusyGroupId] = useState<number | null>(null)

  function reload() {
    if (!raceId) {
      setGroups([])
      return
    }
    setLoading(true)
    setError(null)
    getResultDisputes(raceId)
      .then(setGroups)
      .catch(() => {
        setGroups([])
        setError('No se pudieron cargar los dorsales en disputa. Intentá de nuevo.')
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  async function handleResolve(
    disputeGroupId: number,
    asignaciones: DisputeAssignment[],
    anular: number[],
    razon: string,
  ) {
    if (!raceId) return
    setBusyGroupId(disputeGroupId)
    setError(null)
    try {
      await resolveResultDispute(raceId, disputeGroupId, { asignaciones, anular, razon })
      setGroups((prev) => prev.filter((g) => g.disputeGroupId !== disputeGroupId))
    } catch {
      setError('No se pudo resolver la disputa. Revisá los dorsales y volvé a intentar.')
    } finally {
      setBusyGroupId(null)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-lg font-semibold" style={pageTitle}>
          Dorsales en disputa
        </h1>
        <p className="mt-1 text-sm" style={textLo}>
          Cuando dos capturas reclaman el mismo dorsal, asigná cuál corresponde o anulá la que sobra.
        </p>
      </div>

      {error && (
        <p className="text-sm" role="alert" style={{ color: 'var(--er-tx)' }}>
          {error}
        </p>
      )}

      {!raceId && !loading && (
        <EmptyState message="Elegí una carrera en la barra superior para ver dorsales en disputa." />
      )}

      {loading && <LoadingText message="Cargando disputas…" />}

      {!loading && raceId && groups.length === 0 && (
        <EmptyState message="No hay dorsales en disputa para esta carrera." />
      )}

      {!loading && raceId && groups.length > 0 && (
        <div className="flex flex-col gap-3">
          {groups.map((group) => (
            <DisputeGroupCard
              key={group.disputeGroupId}
              group={group}
              busy={busyGroupId === group.disputeGroupId}
              onResolve={(asignaciones, anular, razon) =>
                handleResolve(group.disputeGroupId, asignaciones, anular, razon)
              }
            />
          ))}
        </div>
      )}
    </div>
  )
}
