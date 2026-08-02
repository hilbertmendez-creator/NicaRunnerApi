import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { LoadingText, ErrorAlert } from '@nicarunner/ui'
import { getPublicRunnerByShareKey } from '../../api/endpoints'
import type { PublicRunnerShareDto } from '../../api/types'
import { RunnerShareCard } from './RunnerShareCard'

interface RunnerShareLocationState {
  fromToken?: string
}

// Container: useParams, useLocation, fetch effect-driven con la bandera `cancelled`
// (mismo patrón que PublicResultsPage.tsx). El link "Volver a resultados" solo aparece
// cuando se llegó navegando desde la tabla (router state) — spec.md "Conditional
// Return-to-Results Link": un visitante que abre la URL compartida directamente (sin
// state, p. ej. desde WhatsApp) nunca ve ese link, porque la URL no lleva ningún token.
export function RunnerSharePage() {
  const { shareKey } = useParams<{ shareKey: string }>()
  const location = useLocation()
  const fromToken = (location.state as RunnerShareLocationState | null)?.fromToken

  const [data, setData] = useState<PublicRunnerShareDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!shareKey) return
    let cancelled = false
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true)
    getPublicRunnerByShareKey(shareKey)
      .then((result) => !cancelled && setData(result))
      .catch((err) => {
        if (cancelled) return
        setError(err.response?.data?.detail ?? 'No se pudo cargar el enlace del corredor.')
      })
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [shareKey])

  return (
    <div className="min-h-screen" style={{ background: 'var(--bg-app)' }}>
      <header
        className="px-6 py-3"
        style={{ background: 'var(--bg-topbar)', borderBottom: '1px solid var(--bd)' }}
      >
        <span className="text-base font-semibold" style={{ color: 'var(--accent)' }}>nicaRunner</span>
        <span className="ml-2 text-sm" style={{ color: 'var(--text-xs)' }}>Corredor</span>
      </header>

      <main className="mx-auto max-w-xl p-6">
        {fromToken && (
          <Link
            to={`/resultados/${fromToken}`}
            className="mb-4 inline-block text-sm font-medium"
            style={{ color: 'var(--accent)' }}
          >
            ← Volver a resultados
          </Link>
        )}

        {loading && <LoadingText message="Cargando..." />}
        {error && <ErrorAlert message={error} />}
        {data && <RunnerShareCard data={data} />}
      </main>
    </div>
  )
}
