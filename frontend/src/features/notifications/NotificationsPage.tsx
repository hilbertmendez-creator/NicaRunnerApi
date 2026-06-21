import { useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { notifyAll } from '../../api/endpoints'
import type { NotifyAllSummaryDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button } from '@nicarunner/ui'

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
        <h1 className="text-lg font-semibold text-gray-900">Notificaciones</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      <section className="rounded-lg bg-white p-5 shadow-sm">
        <h2 className="mb-1 text-sm font-semibold text-gray-900">Notificar a todos los corredores</h2>
        <p className="mb-4 text-sm text-gray-500">
          Envía por Email/WhatsApp el resultado final a cada corredor con tiempo registrado en esta carrera.
        </p>

        {!canNotify && (
          <p className="text-sm text-gray-500">Solo un Administrador puede enviar notificaciones.</p>
        )}

        {canNotify && (
          <Button variant="primary" onClick={handleNotifyAll} disabled={!raceId || sending}>
            {sending ? 'Enviando...' : 'Notificar a todos'}
          </Button>
        )}

        {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

        {summary && (
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div className="rounded-lg bg-gray-50 p-3">
              <p className="text-xs text-gray-500">Resultados</p>
              <p className="text-xl font-medium text-gray-900">{summary.totalResultados}</p>
            </div>
            <div className="rounded-lg bg-orange-50 p-3">
              <p className="text-xs text-orange-700">Creadas</p>
              <p className="text-xl font-medium text-orange-900">{summary.notificacionesCreadas}</p>
            </div>
            <div className="rounded-lg bg-teal-50 p-3">
              <p className="text-xs text-teal-700">Enviadas</p>
              <p className="text-xl font-medium text-teal-900">{summary.enviadas}</p>
            </div>
            <div className="rounded-lg bg-red-50 p-3">
              <p className="text-xs text-red-700">Fallidas</p>
              <p className="text-xl font-medium text-red-900">{summary.fallidas}</p>
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
