import { useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { notifyAll } from '../../api/endpoints'
import type { NotifyAllSummaryDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button } from '@nicarunner/ui'
import { pageTitle, card, cardTitle, textLo, miniMetric } from '../../theme/styles'

export function NotificationsPage() {
  const { user } = useAuth()
  const canNotify = user?.role === 'Administrador'

  const [raceId, setRaceId] = useState<number | null>(null)
  const [summary, setSummary] = useState<NotifyAllSummaryDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  async function handleNotifyAll() {
    if (!raceId) return
    if (!confirm('¿Enviar notificaciones a todos los corredores con resultado registrado?')) return
    setError(null)
    setSending(true)
    try {
      const result = await notifyAll(raceId)
      setSummary(result)
    } catch {
      setError('No se pudieron enviar las notificaciones.')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={pageTitle}>Notificaciones</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      <section style={card}>
        <h2 className="mb-1 text-sm font-semibold" style={cardTitle}>Notificar a todos los corredores</h2>
        <p className="mb-4 text-sm" style={textLo}>
          Envía por Email/WhatsApp el resultado final a cada corredor con tiempo registrado en esta carrera.
        </p>

        {!canNotify && (
          <p className="text-sm" style={textLo}>Solo un Administrador puede enviar notificaciones.</p>
        )}

        {canNotify && (
          <Button variant="primary" onClick={handleNotifyAll} disabled={!raceId || sending}>
            {sending ? 'Enviando...' : 'Notificar a todos'}
          </Button>
        )}

        {error && <p className="mt-3 text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}

        {summary && (
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div style={miniMetric('var(--text-lo)')}>
              <p className="text-xs" style={textLo}>Resultados</p>
              <p className="text-xl font-medium" style={{ color: 'var(--text-hi)' }}>{summary.totalResultados}</p>
            </div>
            <div style={miniMetric('var(--accent)')}>
              <p className="text-xs" style={{ color: 'var(--accent)' }}>Creadas</p>
              <p className="text-xl font-medium" style={{ color: 'var(--accent)' }}>{summary.notificacionesCreadas}</p>
            </div>
            <div style={miniMetric('var(--badge-ok-text)')}>
              <p className="text-xs" style={{ color: 'var(--badge-ok-text)' }}>Enviadas</p>
              <p className="text-xl font-medium" style={{ color: 'var(--badge-ok-text)' }}>{summary.enviadas}</p>
            </div>
            <div style={miniMetric('var(--badge-er-text)')}>
              <p className="text-xs" style={{ color: 'var(--badge-er-text)' }}>Fallidas</p>
              <p className="text-xl font-medium" style={{ color: 'var(--badge-er-text)' }}>{summary.fallidas}</p>
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
