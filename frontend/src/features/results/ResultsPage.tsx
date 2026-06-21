import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { getResults } from '../../api/endpoints'
import type { ResultDto } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
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

  function reload() {
    if (!raceId) return
    setLoading(true)
    getResults(raceId)
      .then(setResults)
      .finally(() => setLoading(false))
  }

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
                    <button
                      onClick={() => setAuditingId(result.id)}
                      className="rounded border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-100"
                    >
                      Auditoría
                    </button>
                    {canEdit && (
                      <button
                        onClick={() => setEditing(result)}
                        className="rounded border border-blue-300 px-2 py-1 text-xs text-blue-700 hover:bg-blue-50"
                      >
                        Editar
                      </button>
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
