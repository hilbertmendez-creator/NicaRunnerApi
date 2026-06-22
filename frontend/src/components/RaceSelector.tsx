import { useEffect, useState } from 'react'
import { getRaces } from '../api/endpoints'
import type { RaceDto } from '../api/types'
import { Select } from '@nicarunner/ui'

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

  if (loading) return <p className="text-sm text-zinc-500">Cargando carreras...</p>

  if (races.length === 0) {
    return <p className="text-sm text-zinc-500">No hay carreras creadas todavía.</p>
  }

  return (
    <Select value={value ?? ''} onChange={(e) => onChange(Number(e.target.value))}>
      {races.map((race) => (
        <option key={race.id} value={race.id}>
          {race.nombre} — {race.estado}
        </option>
      ))}
    </Select>
  )
}
