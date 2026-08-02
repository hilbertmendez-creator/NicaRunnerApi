import type { CSSProperties } from 'react'
import { StatusBadge } from '../../components/StatusBadge'
import { ConnectionStatusBadge, type ConnectionState } from '../../components/ConnectionStatusBadge'
import { getDashboard, getStandings } from '../../api/endpoints'
import { usePolling } from '../../hooks/usePolling'
import { useRaceDashboardHub } from '../../hooks/useRaceDashboardHub'
import { useActiveRace } from '../../hooks/useActiveRace'
import { DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import { KpiBar } from './KpiBar'
import type { Column } from '@nicarunner/ui'
import type { CategoryProgressDto, RecentResultDto, RunnerStandingDto } from '../../api/types'

const POLL_INTERVAL_MS = 5000

type PillRole = 'blue' | 'ok' | 'warn' | 'error'

const PILL_COLORS: Record<PillRole, { bg: string; bd: string; tx: string }> = {
  blue:  { bg: 'var(--in-bg)',  bd: 'var(--in-bd)',  tx: 'var(--in-tx)'  },
  ok:    { bg: 'var(--ok-bg)',  bd: 'var(--ok-bd)',  tx: 'var(--ok-tx)'  },
  warn:  { bg: 'var(--wn-bg)',  bd: 'var(--wn-bd)',  tx: 'var(--wn-tx)'  },
  error: { bg: 'var(--er-bg)',  bd: 'var(--er-bd)',  tx: 'var(--er-tx)'  },
}

function MetricPill({ label, role }: { label: string; role: PillRole }) {
  const c = PILL_COLORS[role]
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        fontSize: 10.5,
        fontWeight: 600,
        textTransform: 'uppercase',
        letterSpacing: '.4px',
        padding: '3px 8px',
        borderRadius: 20,
        background: c.bg,
        border: `1px solid ${c.bd}`,
        color: c.tx,
        marginBottom: 4,
        fontFamily: 'Inter, system-ui',
      }}
    >
      {label}
    </span>
  )
}
const MONO = 'font-mono tabular-nums'

const cardStyle: CSSProperties = {
  background: 'var(--bg-card)',
  border: '1px solid var(--bd)',
  borderRadius: 'var(--r-card)',
  padding: 14,
}
const cardTitleStyle: CSSProperties = { color: 'var(--tx-hi)' }

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

function connectionState(loading: boolean, hasData: boolean, error: unknown): ConnectionState {
  if (error && !hasData) return 'offline'
  if (loading && hasData) return 'syncing'
  return 'online'
}

export function DashboardPage() {
  const { raceId } = useActiveRace()

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

  useRaceDashboardHub(raceId, () => {
    dashboard.refetch()
    standings.refetch()
  })

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
      {/* Race chrome vive solo en TopbarRaceSelect (ActiveRaceProvider). */}
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

      {!raceId && <EmptyState message="Selecciona una carrera para ver su progreso." />}

      {raceId && dashboard.loading && !dashboard.data && (
        <LoadingText message="Cargando dashboard..." />
      )}

      {dashboard.data && (
        <>
          <KpiBar
            items={[
              { label: 'Tiempo en curso', value: '—' },
              { label: 'Ritmo promedio', value: '—' },
              {
                label: 'Chip llegadas',
                value: String(dashboard.data.totalConTiempo),
                trend: `▲ de ${dashboard.data.totalInscritos} inscritos`,
                trendColor:
                  dashboard.data.totalConTiempo > 0 ? 'var(--ok-tx)' : 'var(--tx-lo)',
              },
              {
                label: 'Pendientes',
                value: String(dashboard.data.totalPendientes),
                valueColor: dashboard.data.totalPendientes > 0 ? 'var(--wn-tx)' : undefined,
              },
              { label: 'Cámara ok', value: '—' },
              { label: 'Último dorsal', value: '—' },
            ]}
          />
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--bd)', borderRadius: 'var(--r-card)', padding: '14px 16px', boxShadow: 'var(--shadow-sm)' }}>
              <MetricPill label="Inscritos" role="blue" />
              <div style={{ fontSize: 26, fontWeight: 700, color: 'var(--tx-hi)', letterSpacing: '-.6px', lineHeight: 1, fontFeatureSettings: '"tnum"', fontFamily: '"IBM Plex Mono", ui-monospace, monospace' }}>
                {dashboard.data.totalInscritos}
              </div>
            </div>

            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--bd)', borderRadius: 'var(--r-card)', padding: '14px 16px', boxShadow: 'var(--shadow-sm)' }}>
              <MetricPill label="Con tiempo" role="ok" />
              <div style={{ fontSize: 26, fontWeight: 700, color: 'var(--ok-tx)', letterSpacing: '-.6px', lineHeight: 1, fontFeatureSettings: '"tnum"', fontFamily: '"IBM Plex Mono", ui-monospace, monospace' }}>
                {dashboard.data.totalConTiempo}
              </div>
            </div>

            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--bd)', borderRadius: 'var(--r-card)', padding: '14px 16px', boxShadow: 'var(--shadow-sm)' }}>
              <MetricPill label="Pendientes" role="warn" />
              <div style={{ fontSize: 26, fontWeight: 700, color: 'var(--wn-tx)', letterSpacing: '-.6px', lineHeight: 1, fontFeatureSettings: '"tnum"', fontFamily: '"IBM Plex Mono", ui-monospace, monospace' }}>
                {dashboard.data.totalPendientes}
              </div>
              {dashboard.data.totalPendientes > 0 && (
                <div style={{ fontSize: 11, color: 'var(--tx-lo)', marginTop: 2 }}>requieren atención</div>
              )}
            </div>
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

