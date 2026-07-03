import { useEffect, useState } from 'react'
import type { ResultAuditDto } from '../../api/types'
import { getResultAudit } from '../../api/endpoints'
import { Modal, Button } from '@nicarunner/ui'

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
    <Modal onClose={onClose} maxWidth="lg" labelledBy="audit-history-title">
      <h2 id="audit-history-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
        Auditoría — resultado #{resultId}
      </h2>

      {loading && <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando historial...</p>}

      {!loading && entries.length === 0 && (
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Sin cambios registrados todavía.</p>
      )}

      <ul className="flex flex-col gap-3">
        {entries.map((entry) => (
          <li key={entry.id} className="p-3 text-sm" style={{ border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}>
            <p className="font-medium" style={{ color: 'var(--text-hi)' }}>{entry.campoModificado}</p>
            <p className="font-mono tabular-nums" style={{ color: 'var(--text-lo)' }}>
              {entry.valorAnterior} → {entry.valorNuevo}
            </p>
            {entry.razon && <p className="mt-1" style={{ color: 'var(--text-lo)' }}>Razón: {entry.razon}</p>}
            <p className="mt-1 font-mono text-xs tabular-nums" style={{ color: 'var(--text-xs)' }}>
              {new Date(entry.createdAt).toLocaleString('es-NI')}
            </p>
          </li>
        ))}
      </ul>

      <div className="mt-4 flex justify-end">
        <Button onClick={onClose}>Cerrar</Button>
      </div>
    </Modal>
  )
}
