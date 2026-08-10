import { useEffect, useRef, useState } from 'react'

interface UsePollingResult<T> {
  data: T | null
  error: unknown
  loading: boolean
  refetch: () => void
}

export function usePolling<T>(
  fetcher: () => Promise<T>,
  intervalMs: number,
  deps: unknown[],
): UsePollingResult<T> {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [loading, setLoading] = useState(true)
  const fetcherRef = useRef(fetcher)
  const tickRef = useRef<() => void>(() => {})

  useEffect(() => {
    fetcherRef.current = fetcher
  })

  useEffect(() => {
    let cancelled = false
    // Effect-driven data fetching with a loading flag is the pattern React's own
    // docs recommend (https://react.dev/learn/synchronizing-with-effects#fetching-data).
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true)

    async function tick() {
      // Pestaña en segundo plano: nada que refrescar en pantalla, no vale la
      // pena el request. Se retoma solo (próximo tick) o al instante al
      // volver a la pestaña, vía el listener de visibilitychange de abajo.
      if (document.hidden) return
      try {
        const result = await fetcherRef.current()
        if (!cancelled) {
          setData(result)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) setError(err)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    tickRef.current = tick

    function onVisibilityChange() {
      if (!document.hidden) tick()
    }

    tick()
    const id = setInterval(tick, intervalMs)
    document.addEventListener('visibilitychange', onVisibilityChange)
    return () => {
      cancelled = true
      clearInterval(id)
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  return { data, error, loading, refetch: () => tickRef.current() }
}
