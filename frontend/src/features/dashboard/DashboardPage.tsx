import { useState } from 'react'
import type { CSSProperties } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { StatusBadge } from '../../components/StatusBadge'
import { ConnectionStatusBadge, type ConnectionState } from '../../components/ConnectionStatusBadge'
import { getDashboard, getStandings } from '../../api/endpoints'
import { usePolling } from '../../hooks/usePolling'
import { MetricCard, DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import type { CategoryProgressDto, RecentResultDto, RunnerStandingDto } from '../../api/types'

const POLL_INTERVAL_MS = 5000
const MONO = 'font-mono tabular-nums'

const cardStyle: CSSProperties = {
  background: 'var(--bg-card)',
  border: '1px solid var(--bd-card)',
  borderRadius: 'var(--radius-card)',
  padding: 14,
}
const cardTitleStyle: CSSProperties = { color: 'var(--text-hi)' }

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

function connectionState(loading: boolean, hasData: boolean, error: unknown): ConnectionState {
  if (error && !hasData) return 'offline'
  if (loading && hasData) return 'syncing'
  return 'online'
}

export function DashboardPage() {
  const [raceId, setRaceId] = useState<number | null>(null)

  const dashboard = usePolling(
    () => (raceId ? getDashboard(raceId) : Promise.resolve(null)),
    POLL_INTERVAL_MS,
    [raceId],
  )
  const standings = usePolling(
    () => (raceId ? getStandings(raceId) : Promise.resolve([])),
    POLL_INTERVAL_MS,
    [raceId],
  )

  const ultimosResultadosColumns: Column<RecentResultDto>[] = [
    { header: 'Dorsal', render: (r) => r.dorsal, className: MONO },
    { header: 'Nombre', render: (r) => r.nombre },
    { header: 'Categoría', render: (r) => r.nombreCategoria },
    { header: 'Posición', render: (r) => r.posicion, className: MONO },
    { header: 'Hora', render: (r) => formatTime(r.tiempoLlegada), className: MONO },
  ]

  const categoriasColumns: Column<CategoryProgressDto>[] = [
    { header: 'Categoría', render: (cat) => cat.nombreCategoria },
    { header: 'Inscritos', render: (cat) => cat.inscritos, className: MONO },
    { header: 'Con tiempo', render: (cat) => cat.conTiempo, className: MONO },
    { header: 'Pendientes', render: (cat) => cat.pendientes, className: MONO },
  ]

  const standingsColumns: Column<RunnerStandingDto>[] = [
    { header: 'Pos.', render: (res) => res.posicion, className: MONO },
    { header: 'Dorsal', render: (res) => res.dorsal, className: MONO },
    { header: 'Nombre', render: (res) => res.nombre },
    { header: 'Hora', render: (res) => formatTime(res.tiempoLlegada), className: MONO },
  ]

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-lg font-semibold" style={{ color: 'var(--text-hi)' }}>
            {dashboard.data?.raceName ?? 'Dashboard en vivo'}
          </h1>
          {dashboard.data && <StatusBadge status={dashboard.data.estado} />}
          {raceId && (
            <ConnectionStatusBadge
              state={connectionState(dashboard.loading, dashboard.data !== null, dashboard.error)}
            />
          )}
        </div>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      {!raceId && <EmptyState message="Selecciona una carrera para ver su progreso." />}

      {raceId && dashboard.loading && !dashboard.data && (
        <LoadingText message="Cargando dashboard..." />
      )}

      {dashboard.data && (
        <>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <MetricCard label="Inscritos" value={dashboard.data.totalInscritos} variant="orange" />
            <MetricCard label="Con tiempo" value={dashboard.data.totalConTiempo} variant="teal" />
            <MetricCard label="Pendientes" value={dashboard.data.totalPendientes} variant="amber" />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1.3fr_1fr]">
            <section className="flex flex-col gap-2" style={cardStyle}>
              <h2 className="text-sm font-semibold" style={cardTitleStyle}>Últimos resultados capturados</h2>
              <DataTable
                columns={ultimosResultadosColumns}
                data={dashboard.data.ultimosResultados}
                rowKey={(r) => r.resultId}
                emptyState={<EmptyState message="Sin resultados capturados todavía." />}
              />
            </section>

            <section className="flex flex-col gap-2" style={cardStyle}>
              <h2 className="text-sm font-semibold" style={cardTitleStyle}>Progreso por categoría</h2>
              <DataTable
                columns={categoriasColumns}
                data={dashboard.data.categorias}
                rowKey={(cat) => cat.categoryId}
              />
            </section>
          </div>
        </>
      )}

      {standings.data && standings.data.length > 0 && (
        <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {standings.data.map((cat) => (
            <div key={cat.categoryId} className="flex flex-col gap-2" style={cardStyle}>
              <h3 className="text-sm font-semibold" style={cardTitleStyle}>
                {cat.nombreCategoria} ({cat.distancia} km)
              </h3>
              <DataTable
                columns={standingsColumns}
                data={cat.resultados}
                rowKey={(res) => res.runnerId}
              />
            </div>
          ))}
        </section>
      )}
    </div>
  )
}

