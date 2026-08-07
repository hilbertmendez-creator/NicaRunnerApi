import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { useRace } from '../../hooks/useRace'
import { getDashboard, notifyAll } from '../../api/endpoints'
import type { NotifyAllSummaryDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, MetricCard, SectionHeader } from '@nicarunner/ui'
import { card, cardTitle, textLo } from '../../theme/styles'

export function NotificationsPage() {
  const { user } = useAuth()
  const canNotify = user?.role === 'Administrador'

  const { raceId } = useRace()
  const [conTiempo, setConTiempo] = useState<number | null>(null)
  const [sinTiempo, setSinTiempo] = useState<number | null>(null)
  const [summary, setSummary] = useState<NotifyAllSummaryDto | null>(null)
  const [sentAt, setSentAt] = useState<Date | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  useEffect(() => {
    if (!raceId) return
    let cancelled = false
    // Pre-send KPIs: reutilizamos el dashboard existente (sin contrato nuevo).
    getDashboard(raceId)
      .then((d) => {
        if (cancelled) return
        setConTiempo(d.totalConTiempo)
        setSinTiempo(d.totalPendientes)
      })
      .catch(() => {
        if (cancelled) return
        setConTiempo(null)
        setSinTiempo(null)
      })
    return () => {
      cancelled = true
    }
  }, [raceId])

  async function handleNotifyAll() {
    if (!raceId) return
    if (!confirm('¿Enviar notificaciones a todos los corredores con tiempo registrado?')) return
    setError(null)
    setSending(true)
    try {
      const result = await notifyAll(raceId)
      setSummary(result)
      setSentAt(new Date())
    } catch {
      setError('No se pudieron enviar las notificaciones.')
    } finally {
      setSending(false)
    }
  }

  const targetCount = conTiempo ?? summary?.totalResultados ?? 0

  return (
    <div className="flex flex-col gap-4">
      <SectionHeader
        title="Envía los tiempos oficiales"
        subtitle="Por email y WhatsApp a todos los corredores con tiempo registrado."
        actions={<RaceSelector />}
      />

      <section style={card}>
        {/* Info banner */}
        <div
          className="mb-4 flex items-center gap-3 px-4 py-3"
          style={{ background: 'var(--info-50)', border: '1px solid var(--info-200)', borderRadius: 'var(--radius-btn)' }}
        >
          <span style={{ color: 'var(--info-600)' }}>
            Se enviarán notificaciones a todos los corredores con tiempo oficial registrado. Los
            corredores sin tiempo no recibirán notificación.
          </span>
        </div>

        {/* Pre-send KPIs */}
        <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <MetricCard
              label="Corredores con tiempo"
              value={conTiempo ?? '—'}
              variant="neutral"
            />
            <p className="mt-1 text-xs" style={textLo}>Recibirán notificación</p>
          </div>
          <div>
            <MetricCard label="Sin tiempo registrado" value={sinTiempo ?? '—'} variant="neutral" />
            <p className="mt-1 text-xs" style={textLo}>No recibirán notificación</p>
          </div>
        </div>

        {!canNotify && (
          <p className="mb-4 text-sm" style={textLo}>Solo un Administrador puede enviar notificaciones.</p>
        )}

        {canNotify && (
          <Button
            variant="primary"
            className="w-full"
            onClick={handleNotifyAll}
            disabled={!raceId || sending}
          >
            {sending
              ? 'Enviando...'
              : `Enviar notificaciones a ${targetCount} corredores`}
          </Button>
        )}

        {error && <p className="mt-3 text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}
      </section>

      {/* Post-dispatch metrics */}
      {summary && (
        <section style={card}>
          <h2 className="mb-3 text-sm font-semibold" style={cardTitle}>
            Último envío{sentAt ? ` — ${sentAt.toLocaleString('es-NI')}` : ''}
          </h2>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <MetricCard label="Creadas" value={summary.notificacionesCreadas} variant="neutral" />
            <MetricCard label="Enviadas" value={summary.enviadas} variant="neutral" />
            <MetricCard label="Fallidas" value={summary.fallidas} variant="neutral" />
            <MetricCard
              label="Pendientes"
              value={Math.max(0, summary.notificacionesCreadas - summary.enviadas - summary.fallidas)}
              variant="neutral"
            />
          </div>
        </section>
      )}

      <p className="text-center text-xs" style={textLo}>
        Email + WhatsApp · Solo administradores pueden ejecutar esta acción
      </p>
    </div>
  )
}
