import { useEffect, useState } from 'react'
import { getRaces } from '../api/endpoints'
import type { RaceDto } from '../api/types'

interface RaceSelectorProps {
  value: number | null
  onChange: (raceId: number) => void
}

export function RaceSelector({ value, onChange }: RaceSelectorProps) {
  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    getRaces()
      .then((data) => {
        if (cancelled) return
        setRaces(data)
        if (data.length > 0 && value === null) {
          onChange(data[0].id)
        }
      })
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (loading) return <p className="text-sm text-gray-500">Cargando carreras...</p>

  if (races.length === 0) {
    return <p className="text-sm text-gray-500">No hay carreras creadas todavía.</p>
  }

  return (
    <select
      value={value ?? ''}
      onChange={(e) => onChange(Number(e.target.value))}
      className="rounded border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
    >
      {races.map((race) => (
        <option key={race.id} value={race.id}>
          {race.nombre} — {race.estado}
        </option>
      ))}
    </select>
  )
}
