import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { getResults, notifyResult } from '../../api/endpoints'
import type { ResultDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button } from '../../components/Button'
import { EditResultModal } from './EditResultModal'
import { AuditHistory } from './AuditHistory'

export function ResultsPage() {
  const { user } = useAuth()
  const canEdit = user?.role === 'Administrador'

  const [raceId, setRaceId] = useState<number | null>(null)
  const [results, setResults] = useState<ResultDto[]>([])
  const [loading, setLoading] = useState(false)
  const [editing, setEditing] = useState<ResultDto | null>(null)
  const [auditingId, setAuditingId] = useState<number | null>(null)
  const [notifyingId, setNotifyingId] = useState<number | null>(null)

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
    getResults(raceId)
      .then(setResults)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold text-gray-900">Resultados</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      {loading && <p className="text-sm text-gray-500">Cargando resultados...</p>}

      {!loading && raceId && results.length === 0 && (
        <p className="text-sm text-gray-500">Esta carrera no tiene resultados capturados todavía.</p>
      )}

      {results.length > 0 && (
        <section className="overflow-x-auto rounded-lg bg-white p-4 shadow-sm">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-gray-500">
                <th className="py-1">Posición</th>
                <th className="py-1">Dorsal</th>
                <th className="py-1">Hora de llegada</th>
                <th className="py-1">Última edición</th>
                <th className="py-1"></th>
              </tr>
            </thead>
            <tbody>
              {results.map((result) => (
                <tr key={result.id} className="border-t border-gray-100">
                  <td className="py-1.5">{result.posicion}</td>
                  <td className="py-1.5">{result.dorsal}</td>
                  <td className="py-1.5">{new Date(result.tiempoLlegada).toLocaleString('es-NI')}</td>
                  <td className="py-1.5">{new Date(result.updatedAt).toLocaleString('es-NI')}</td>
                  <td className="flex gap-2 py-1.5">
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
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
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
