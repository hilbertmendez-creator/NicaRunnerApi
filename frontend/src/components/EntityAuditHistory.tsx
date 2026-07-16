import { useEffect, useState } from 'react'
import type { AuditLogDto } from '../api/types'
import { Modal, Button } from '@nicarunner/ui'

interface EntityAuditHistoryProps {
  title: string
  load: () => Promise<AuditLogDto[]>
  onClose: () => void
}

// Historial de bitácora genérico, reutilizable para Usuarios/Carreras/Categorías.
export function EntityAuditHistory({ title, load, onClose }: EntityAuditHistoryProps) {
  const [entries, setEntries] = useState<AuditLogDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    load()
      .then((data) => !cancelled && setEntries(data))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <Modal onClose={onClose} maxWidth="lg" labelledBy="entity-audit-history-title">
      <h2 id="entity-audit-history-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
        {title}
      </h2>

      {loading && <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando historial...</p>}

      {!loading && entries.length === 0 && (
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Sin cambios registrados todavía.</p>
      )}

      <ul className="flex flex-col gap-3">
        {entries.map((entry) => (
          <li key={entry.id} className="p-3 text-sm" style={{ border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}>
            <p className="font-medium" style={{ color: 'var(--text-hi)' }}>{entry.campo}</p>
            <p className="font-mono tabular-nums" style={{ color: 'var(--text-lo)' }}>
              {entry.valorAnterior ?? '(sin valor)'} → {entry.valorNuevo ?? '(sin valor)'}
            </p>
            <p className="mt-1" style={{ color: 'var(--text-lo)' }}>Modificado por: {entry.autorNombre}</p>
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
