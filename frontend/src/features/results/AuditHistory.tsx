import { useEffect, useState } from 'react'
import type { ResultAuditDto } from '../../api/types'
import { getResultAudit } from '../../api/endpoints'

interface AuditHistoryProps {
  raceId: number
  resultId: number
  onClose: () => void
}

export function AuditHistory({ raceId, resultId, onClose }: AuditHistoryProps) {
  const [entries, setEntries] = useState<ResultAuditDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    getResultAudit(raceId, resultId)
      .then((data) => !cancelled && setEntries(data))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [raceId, resultId])

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-base font-semibold text-gray-900">
          Auditoría — resultado #{resultId}
        </h2>

        {loading && <p className="text-sm text-gray-500">Cargando historial...</p>}

        {!loading && entries.length === 0 && (
          <p className="text-sm text-gray-500">Sin cambios registrados todavía.</p>
        )}

        <ul className="flex flex-col gap-3">
          {entries.map((entry) => (
            <li key={entry.id} className="rounded border border-gray-200 p-3 text-sm">
              <p className="font-medium text-gray-900">{entry.campoModificado}</p>
              <p className="text-gray-600">
                {entry.valorAnterior} → {entry.valorNuevo}
              </p>
              {entry.razon && <p className="mt-1 text-gray-500">Razón: {entry.razon}</p>}
              <p className="mt-1 text-xs text-gray-400">
                {new Date(entry.createdAt).toLocaleString('es-NI')}
              </p>
            </li>
          ))}
        </ul>

        <div className="mt-4 flex justify-end">
          <button
            onClick={onClose}
            className="rounded border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>
  )
}
