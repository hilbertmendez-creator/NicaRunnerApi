import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { getRaces } from '../api/endpoints'
import type { RaceDto } from '../api/types'

const STORAGE_KEY = 'nicarunner-active-race-id'

interface ActiveRaceContextValue {
  raceId: number | null
  races: RaceDto[]
  activeRace: RaceDto | null
  loading: boolean
  setRaceId: (raceId: number) => void
}

const ActiveRaceContext = createContext<ActiveRaceContextValue | null>(null)

function readStoredId(): number | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const id = Number(raw)
    return Number.isFinite(id) ? id : null
  } catch {
    return null
  }
}

/** Prefiere la carrera guardada, luego EnCurso, luego la primera. */
function pickDefaultRace(races: RaceDto[], preferredId: number | null): number | null {
  if (preferredId != null && races.some((r) => r.id === preferredId)) return preferredId
  const enCurso = races.find((r) => r.estado === 'EnCurso')
  if (enCurso) return enCurso.id
  return races[0]?.id ?? null
}

export function ActiveRaceProvider({ children }: { children: ReactNode }) {
  const [raceId, setRaceIdState] = useState<number | null>(readStoredId)
  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    getRaces()
      .then((data) => {
        if (cancelled) return
        setRaces(data)
        const next = pickDefaultRace(data, readStoredId())
        setRaceIdState(next)
        if (next != null) {
          try {
            localStorage.setItem(STORAGE_KEY, String(next))
          } catch {
            /* ignore quota / private mode */
          }
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const setRaceId = useCallback((id: number) => {
    setRaceIdState(id)
    try {
      localStorage.setItem(STORAGE_KEY, String(id))
    } catch {
      /* ignore quota / private mode */
    }
  }, [])

  const activeRace = useMemo(
    () => races.find((r) => r.id === raceId) ?? null,
    [races, raceId],
  )

  const value = useMemo(
    () => ({ raceId, races, activeRace, loading, setRaceId }),
    [raceId, races, activeRace, loading, setRaceId],
  )

  return <ActiveRaceContext.Provider value={value}>{children}</ActiveRaceContext.Provider>
}

export function useActiveRace(): ActiveRaceContextValue {
  const ctx = useContext(ActiveRaceContext)
  if (!ctx) throw new Error('useActiveRace debe usarse dentro de ActiveRaceProvider')
  return ctx
}
