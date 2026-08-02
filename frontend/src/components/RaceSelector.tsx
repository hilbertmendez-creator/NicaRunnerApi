import { useActiveRace } from '../hooks/useActiveRace'
import { Select } from '@nicarunner/ui'

/** Selector de carrera cableado al ActiveRace compartido (topbar + páginas). */
export function RaceSelector() {
  const { raceId, races, loading, setRaceId } = useActiveRace()

  if (loading) {
    return (
      <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
        Cargando carreras...
      </p>
    )
  }

  if (races.length === 0) {
    return (
      <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
        No hay carreras creadas todavía.
      </p>
    )
  }

  return (
    <Select value={raceId ?? ''} onChange={(e) => setRaceId(Number(e.target.value))}>
      {races.map((race) => (
        <option key={race.id} value={race.id}>
          {race.nombre} — {race.estado}
        </option>
      ))}
    </Select>
  )
}
