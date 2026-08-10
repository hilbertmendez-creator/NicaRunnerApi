import { useEffect, useState } from 'react'
import { getResults, notifyResult } from '../../api/endpoints'
import type { ResultDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { useActiveRace } from '../../hooks/useActiveRace'
import { Button, DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { PositionBadge } from '../../components/PositionBadge'
import { EditResultModal } from './EditResultModal'
import { AuditHistory } from './AuditHistory'
import { pageTitle } from '../../theme/styles'

export function ResultsPage() {
  const { user } = useAuth()
  const canEdit = user?.role === 'Administrador'
  const { raceId } = useActiveRace()

  const [results, setResults] = useState<ResultDto[]>([])
  const [loading, setLoading] = useState(false)
  const [editing, setEditing] = useState<ResultDto | null>(null)
  const [auditingId, setAuditingId] = useState<number | null>(null)
  const [notifyingId, setNotifyingId] = useState<number | null>(null)

  const [pageIndex, setPageIndex] = useState(1) // 1-based; DataTable contract
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 50

  async function handleNotify(resultId: number) {
    setNotifyingId(resultId)
    try {
      await notifyResult(resultId)
    } finally {
      setNotifyingId(null)
    }
  }

  function reload() {
    if (!raceId) return
    setLoading(true)
    getResults(raceId, pageSize, (pageIndex - 1) * pageSize)
      .then((res) => {
        setResults(res.items)
        setTotalCount(res.totalCount)
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId, pageIndex])

  const columns: Column<ResultDto>[] = [
    {
      header: 'Posición',
      render: (result) => <PositionBadge position={result.posicion} />,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Dorsal',
      render: (result) => result.dorsal,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Hora de llegada',
      render: (result) => new Date(result.tiempoLlegada).toLocaleString('es-NI'),
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
              <Button size="sm" onClick={() => setEditing(result)}>
                Editar
              </Button>
              <Button
                size="sm"
                variant="info"
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
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={pageTitle}>Resultados</h1>
      </div>

      {loading && <LoadingText message="Cargando resultados..." />}

      {!loading && raceId && (
        <DataTable
          columns={columns}
          data={results}
          rowKey={(result) => result.id}
          emptyState={<EmptyState message="Esta carrera no tiene resultados capturados todavía." />}
          pageIndex={pageIndex}
          pageCount={Math.ceil(totalCount / pageSize)}
          onPageChange={setPageIndex}
          wrapperClassName="no-stagger"
        />
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
