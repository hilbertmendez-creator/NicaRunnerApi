import { useEffect, useRef, type CSSProperties } from 'react'
import { StatusBadge } from '../../components/StatusBadge'
import { ConnectionStatusBadge, type ConnectionState } from '../../components/ConnectionStatusBadge'
import { PositionBadge } from '../../components/PositionBadge'
import { getDashboard, getStandings } from '../../api/endpoints'
import { usePolling } from '../../hooks/usePolling'
import { useRaceDashboardHub } from '../../hooks/useRaceDashboardHub'
import { useActiveRace } from '../../hooks/useActiveRace'
import { DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import { KpiBar } from './KpiBar'
import type { Column } from '@nicarunner/ui'
import type { CategoryProgressDto, RecentResultDto, RunnerStandingDto } from '../../api/types'
import { cardTitle, pageTitle } from '../../theme/styles'
import { formatElapsed } from '../public-results/formatElapsed'

const POLL_INTERVAL_MS = 5000
// El hub SignalR ya empuja los cambios en tiempo real; con el hub sano este
// polling es solo una red de seguridad y no necesita correr cada 5s.
const IDLE_POLL_INTERVAL_MS = 20000

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

  // Refs porque useRaceDashboardHub necesita el callback antes de que
  // dashboard/standings existan (usePolling se declara después, ya que su
  // intervalo depende de hubConnected) — se actualizan en cada render, se
  // invocan solo async cuando el hub avisa "resultsChanged".
  const dashboardRefetchRef = useRef<() => void>(() => {})
  const standingsRefetchRef = useRef<() => void>(() => {})

  const { connected: hubConnected } = useRaceDashboardHub(raceId, () => {
    dashboardRefetchRef.current()
    standingsRefetchRef.current()
  })

  const pollIntervalMs = hubConnected ? IDLE_POLL_INTERVAL_MS : POLL_INTERVAL_MS

  const dashboard = usePolling(
    () => (raceId ? getDashboard(raceId) : Promise.resolve(null)),
    pollIntervalMs,
    [raceId, pollIntervalMs],
  )
  const standings = usePolling(
    () => (raceId ? getStandings(raceId) : Promise.resolve([])),
    pollIntervalMs,
    [raceId, pollIntervalMs],
  )

  // Mismo patrón de "latest ref" que usan usePolling y useRaceDashboardHub:
  // se asigna en un efecto, no durante el render. Un render descartado por el
  // renderer concurrente no debe dejar el ref apuntando a un callback que nunca
  // se commiteó. El hub solo invoca estos refs de forma asíncrona, así que la
  // ventana entre render y commit no es alcanzable — y aunque lo fuera, el
  // refetch de usePolling es () => tickRef.current(), que siempre delega en el
  // tick vigente.
  useEffect(() => {
    dashboardRefetchRef.current = dashboard.refetch
    standingsRefetchRef.current = standings.refetch
  })

  const ultimosResultadosColumns: Column<RecentResultDto>[] = [
    { header: 'Dorsal', render: (r) => r.dorsal, className: MONO },
    { header: 'Nombre', render: (r) => r.nombre },
    { header: 'Categoría', render: (r) => r.nombreCategoria },
    { header: 'Posición', render: (r) => <PositionBadge position={r.posicion} />, className: MONO },
    { header: 'Hora', render: (r) => formatTime(r.tiempoLlegada), className: MONO },
  ]

  const categoriasColumns: Column<CategoryProgressDto>[] = [
    { header: 'Categoría', render: (cat) => cat.nombreCategoria },
    { header: 'Inscritos', render: (cat) => cat.inscritos, className: MONO },
    { header: 'Con tiempo', render: (cat) => cat.conTiempo, className: MONO },
    { header: 'Pendientes', render: (cat) => cat.pendientes, className: MONO },
  ]

  const standingsColumns: Column<RunnerStandingDto>[] = [
    { header: 'Pos.', render: (res) => <PositionBadge position={res.posicion} />, className: MONO },
    { header: 'Dorsal', render: (res) => res.dorsal, className: MONO },
    { header: 'Nombre', render: (res) => res.nombre },
    { header: 'Hora', render: (res) => formatTime(res.tiempoLlegada), className: MONO },
  ]

  return (
    <div className="flex flex-col gap-5">
      {/* Race chrome vive solo en TopbarRaceSelect (ActiveRaceProvider). */}
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={pageTitle}>
          {dashboard.data?.raceName ?? 'Dashboard en vivo'}
        </h1>
        {dashboard.data && <StatusBadge status={dashboard.data.estado} />}
        {raceId && (
          <ConnectionStatusBadge
            state={connectionState(dashboard.loading, dashboard.data !== null, dashboard.error)}
          />
        )}
      </div>

      {!raceId && <EmptyState message="Elegí una carrera en la barra superior para ver su progreso." />}

      {raceId && dashboard.loading && !dashboard.data && (
        <LoadingText message="Cargando progreso de la carrera…" />
      )}

      {dashboard.data && (
        <>
          <KpiBar
            items={[
              {
                label: 'Tiempo en curso',
                value: formatElapsed(dashboard.data.tiempoEnCursoSegundos) ?? '—',
                trend: dashboard.data.tiempoEnCursoSegundos === null ? 'Carrera no iniciada' : undefined,
                kind: 'live',
              },
              {
                label: 'Ritmo promedio',
                value:
                  dashboard.data.ritmoPromedioSegundosPorKm === null
                    ? '—'
                    : `${formatElapsed(dashboard.data.ritmoPromedioSegundosPorKm)}/km`,
                trend: dashboard.data.ritmoPromedioSegundosPorKm === null ? 'Sin tiempos registrados' : undefined,
                kind: 'live',
              },
              {
                label: 'Chip llegadas',
                value: String(dashboard.data.totalConTiempo),
                trend: `▲ de ${dashboard.data.totalInscritos} inscritos`,
                trendColor:
                  dashboard.data.totalConTiempo > 0 ? 'var(--ok-tx)' : 'var(--tx-lo)',
                kind: 'live',
              },
              {
                label: 'Pendientes',
                value: String(dashboard.data.totalPendientes),
                valueColor: dashboard.data.totalPendientes > 0 ? 'var(--wn-tx)' : undefined,
                kind: 'live',
              },
              {
                label: 'Último dorsal',
                value: dashboard.data.ultimosResultados[0]?.dorsal ?? '—',
                trend: dashboard.data.ultimosResultados[0] ? undefined : 'Sin capturas todavía',
                kind: 'live',
              },
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
              <h2 className="text-sm font-semibold" style={cardTitle}>Últimos resultados capturados</h2>
              <DataTable
                columns={ultimosResultadosColumns}
                data={dashboard.data.ultimosResultados}
                rowKey={(r) => r.resultId}
                emptyState={<EmptyState message="Sin resultados capturados todavía." />}
                wrapperClassName="no-stagger"
              />
            </section>

            <section className="flex flex-col gap-2" style={cardStyle}>
              <h2 className="text-sm font-semibold" style={cardTitle}>Progreso por categoría</h2>
              <DataTable
                columns={categoriasColumns}
                data={dashboard.data.categorias}
                rowKey={(cat) => cat.categoryId}
                wrapperClassName="no-stagger"
              />
            </section>
          </div>
        </>
      )}

      {standings.data && standings.data.length > 0 && (
        <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {standings.data.map((cat) => (
            <div key={cat.categoryId} className="flex flex-col gap-2" style={cardStyle}>
              <h3 className="text-sm font-semibold" style={cardTitle}>
                {cat.nombreCategoria} ({cat.distancia} km)
              </h3>
              <DataTable
                columns={standingsColumns}
                data={cat.resultados}
                rowKey={(res) => res.runnerId}
                wrapperClassName="no-stagger"
              />
            </div>
          ))}
        </section>
      )}
    </div>
  )
}

