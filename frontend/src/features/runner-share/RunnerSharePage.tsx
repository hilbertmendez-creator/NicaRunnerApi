import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getPublicRunnerResult } from '../../api/endpoints'
import type { PublicRunnerDetailDto } from '../../api/types'
import { decodeShareKey } from './shareKey'

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

function ordinal(n: number) {
  return `${n}°`
}

function NotFound({ backHref }: { backHref?: string }) {
  return (
    <div className="flex min-h-screen items-center justify-center p-6" style={{ background: 'var(--bg-app)' }}>
      <div
        className="w-full max-w-md p-8 text-center"
        style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
      >
        <p className="text-sm font-medium" style={{ color: 'var(--text-lo)' }}>Corredor no encontrado</p>
        <p className="mt-1 text-xs" style={{ color: 'var(--text-lo)' }}>
          El enlace no es válido o el corredor ya no está disponible.
        </p>
        {backHref && (
          <Link
            to={backHref}
            className="mt-4 inline-block text-xs font-medium underline"
            style={{ color: 'var(--accent)' }}
          >
            &larr; Volver a resultados
          </Link>
        )}
      </div>
    </div>
  )
}

export function RunnerSharePage() {
  const { shareKey } = useParams<{ shareKey: string }>()
  const decoded = useMemo(() => (shareKey ? decodeShareKey(shareKey) : null), [shareKey])
  const [detail, setDetail] = useState<PublicRunnerDetailDto | null>(null)
  const [error, setError] = useState<boolean>(false)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!decoded) return
    let cancelled = false
    // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true)
    getPublicRunnerResult(decoded.token, decoded.runnerId)
      .then((result) => !cancelled && setDetail(result))
      .catch(() => {
        if (cancelled) return
        setError(true)
      })
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [decoded])

  const backHref = decoded ? `/resultados/${decoded.token}` : undefined

  if (!decoded) {
    return <NotFound backHref={backHref} />
  }

  if (loading) {
    return (
      <div className="min-h-screen" style={{ background: 'var(--bg-app)' }}>
        <main className="mx-auto max-w-xl p-6">
          <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando corredor...</p>
        </main>
      </div>
    )
  }

  if (error || !detail) {
    return <NotFound backHref={backHref} />
  }

  return (
    <div className="min-h-screen" style={{ background: 'var(--bg-app)' }}>
      {/* Hero navy (referencia runner-share.html .runner-hero) */}
      <header className="px-6 pb-12 pt-12 text-center" style={{ background: 'var(--bg-sidebar)' }}>
        <div className="font-mono text-6xl font-bold leading-none" style={{ color: 'var(--accent)' }}>
          {detail.dorsal}
        </div>
        <h1 className="mt-4 text-xl font-bold" style={{ color: 'var(--sb-text)' }}>{detail.nombre}</h1>
        <p className="mt-1 text-sm" style={{ color: 'var(--sb-muted)' }}>
          {detail.raceName} &middot; {detail.distancia} km
        </p>
      </header>

      <main className="mx-auto max-w-xl p-6">
        <div
          className="mb-6"
          style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
        >
          <div className="border-b px-5 py-3" style={{ borderColor: 'var(--bd-row)' }}>
            <span className="text-sm font-semibold" style={{ color: 'var(--text-hi)' }}>Detalle del resultado</span>
          </div>
          <div className="px-5">
            <div
              className="flex items-center justify-between py-3"
              style={{ borderBottom: '1px solid var(--bd-row)', color: 'var(--text-hi)' }}
            >
              <span className="text-sm" style={{ color: 'var(--text-lo)' }}>Posición general</span>
              <span className="font-mono text-sm font-medium">{ordinal(detail.posicion)}</span>
            </div>
            <div
              className="flex items-center justify-between py-3"
              style={{ borderBottom: '1px solid var(--bd-row)', color: 'var(--text-hi)' }}
            >
              <span className="text-sm" style={{ color: 'var(--text-lo)' }}>Categoría</span>
              <span className="font-mono text-sm font-medium">{detail.nombreCategoria}</span>
            </div>
            <div className="flex items-center justify-between py-3" style={{ color: 'var(--text-hi)' }}>
              <span className="text-sm" style={{ color: 'var(--text-lo)' }}>Tiempo oficial</span>
              <span className="font-mono text-sm font-medium">{formatTime(detail.tiempoLlegada)}</span>
            </div>
          </div>
        </div>

        <div className="text-center">
          {backHref && (
            <Link
              to={backHref}
              className="inline-block text-sm font-medium underline"
              style={{ color: 'var(--accent)' }}
            >
              &larr; Volver a resultados
            </Link>
          )}
        </div>

        <p className="mt-8 text-center text-xs" style={{ color: 'var(--text-lo)' }}>
          Resultados oficiales de NicaRunner &middot; Cronometraje certificado
        </p>
      </main>
    </div>
  )
}