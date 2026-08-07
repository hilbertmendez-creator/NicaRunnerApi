import { createContext, useContext } from 'react'
import type { RaceDto } from '../api/types'

export interface RaceContextValue {
  races: RaceDto[]
  loading: boolean
  raceId: number | null
  setRaceId: (raceId: number) => void
  selectedRace: RaceDto | undefined
}

export const RaceContext = createContext<RaceContextValue | null>(null)

export function useRace(): RaceContextValue {
  const ctx = useContext(RaceContext)
  if (!ctx) throw new Error('useRace debe usarse dentro de RaceProvider')
  return ctx
}
