import { useActiveRace } from '../hooks/useActiveRace'
import type { RaceStatus } from '../api/types'

function statusDotColor(estado: RaceStatus | undefined): string {
  if (estado === 'EnCurso') return '#22C55E'
  if (estado === 'Planeada') return 'var(--wn-tx)'
  if (estado === 'Terminada') return 'var(--tx-lo)'
  return 'var(--tx-lo)'
}

/** Selector compacto de carrera activa para la topbar. */
export function TopbarRaceSelect() {
  const { raceId, races, activeRace, loading, setRaceId } = useActiveRace()

  const emptyLabel = loading
    ? 'Cargando…'
    : races.length === 0
      ? 'Sin carreras'
      : 'Elegir carrera'

  return (
    <label
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 5,
        background: 'var(--bg-input)',
        border: '1px solid var(--bd)',
        borderRadius: 'var(--r-btn)',
        padding: '4px 10px 4px 8px',
        fontSize: 12,
        fontWeight: 500,
        color: 'var(--tx-hi)',
        fontFamily: 'Inter, system-ui',
        minHeight: 32,
        cursor: races.length === 0 ? 'default' : 'pointer',
      }}
    >
      <span
        aria-hidden="true"
        style={{
          width: 6,
          height: 6,
          borderRadius: '50%',
          background: statusDotColor(activeRace?.estado),
          flexShrink: 0,
          display: 'inline-block',
        }}
      />
      <select
        aria-label="Carrera activa"
        value={raceId ?? ''}
        disabled={loading || races.length === 0}
        onChange={(e) => setRaceId(Number(e.target.value))}
        style={{
          border: 'none',
          background: 'transparent',
          color: 'inherit',
          font: 'inherit',
          cursor: 'inherit',
          maxWidth: 220,
          outline: 'none',
          appearance: 'none',
          paddingRight: 14,
        }}
      >
        {races.length === 0 ? (
          <option value="">{emptyLabel}</option>
        ) : (
          races.map((race) => (
            <option key={race.id} value={race.id}>
              {race.nombre}
            </option>
          ))
        )}
      </select>
      <svg
        width="10"
        height="10"
        viewBox="0 0 10 10"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        aria-hidden="true"
        style={{ marginLeft: -10, pointerEvents: 'none' }}
      >
        <path d="M2 3.5l3 3 3-3" />
      </svg>
    </label>
  )
}
