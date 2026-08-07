import { useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getRaces } from '../api/endpoints'
import type { RaceDto } from '../api/types'
import { RaceContext } from '../hooks/useRace'

/**
 * Levanta el `raceId` activo a nivel de shell (D2): el chip de la topbar
 * refleja la carrera activa y cada página con RaceSelector comparte la
 * misma selección. Mantiene el comportamiento previo: se selecciona la
 * primera carrera por defecto.
 */
export function RaceProvider({ children }: { children: ReactNode }) {
  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [raceId, setRaceId] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    getRaces()
      .then((data) => {
        if (cancelled) return
        setRaces(data)
        if (data.length > 0) {
          setRaceId((current) => current ?? data[0].id)
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo(
    () => ({
      races,
      loading,
      raceId,
      setRaceId,
      selectedRace: races.find((race) => race.id === raceId),
    }),
    [races, loading, raceId],
  )

  return <RaceContext.Provider value={value}>{children}</RaceContext.Provider>
}
