import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getPublicResults } from '../../api/endpoints'
import type { PublicRaceResultsDto, PublicRunnerResultDto } from '../../api/types'
import { Badge } from '@nicarunner/ui'
import { encodeShareKey } from '../runner-share/shareKey'

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

function matches(runner: PublicRunnerResultDto, q: string) {
  if (!q) return true
  return (
    runner.dorsal.toLowerCase().includes(q) ||
    runner.nombre.toLowerCase().includes(q) ||
    formatTime(runner.tiempoLlegada).toLowerCase().includes(q)
  )
}

export function PublicResultsPage() {
  const { token } = useParams<{ token: string }>()
  const [data, setData] = useState<PublicRaceResultsDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState('')

  useEffect(() => {
    if (!token) return
    let cancelled = false
    // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true)
    getPublicResults(token)
      .then((result) => !cancelled && setData(result))
      .catch((err) => {
        if (cancelled) return
        setError(err.response?.data?.detail ?? 'No se pudo cargar el enlace de resultados.')
      })
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [token])

  return (
    <div className="min-h-screen" style={{ background: 'var(--bg-app)' }}>
      {/* Sticky hero: header + filtered search (referencia public-results.html) */}
      <div className="sticky top-0 z-20" style={{ background: 'var(--bg-sidebar)' }}>
        <header className="px-6 pb-4 pt-8 text-center">
          <span className="text-lg font-semibold" style={{ color: 'var(--sb-text)' }}>nicaRunner</span>
          <h1 className="mt-2 text-xl font-bold" style={{ color: '#fff' }}>
            {data?.raceName ?? 'Resultados'}
          </h1>
          <p className="text-sm" style={{ color: 'var(--sb-muted)' }}>
            {data ? new Date(data.fechaCarrera).toLocaleDateString('es-NI') : ''}
          </p>
        </header>
        <div className="px-6 pb-4">
          <div className="relative mx-auto max-w-3xl">
            <svg
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2"
              width="16"
              height="16"
              viewBox="0 0 16 16"
              fill="none"
              style={{ color: 'var(--sb-muted)' }}
            >
              <circle cx="7" cy="7" r="4.5" stroke="currentColor" strokeWidth="1.5" />
              <path d="M10.5 10.5L14 14" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
            </svg>
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Filtrar por dorsal, nombre o tiempo..."
              className="h-9 w-full rounded border pl-9 text-sm"
              style={{
                background: 'rgba(255,255,255,.08)',
                borderColor: 'rgba(255,255,255,.12)',
                color: '#fff',
                outline: 'none',
              }}
              autoComplete="off"
            />
          </div>
        </div>
      </div>

      <main className="mx-auto max-w-3xl p-4">
        {loading && <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando resultados...</p>}

        {error && (
          <div
            className="p-6 text-center"
            style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
          >
            <p className="text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>
          </div>
        )}

        {data && data.categorias.length === 0 && (
          <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Todavía no hay resultados publicados.</p>
        )}

        {data?.categorias.map((cat) => {
          const visible = cat.resultados.filter((r) => matches(r, query.trim().toLowerCase()))
          const filtering = query.trim().length > 0
          return (
            <section
              key={cat.nombreCategoria}
              className="mb-6 p-4"
              style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
            >
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-sm font-semibold" style={{ color: 'var(--text-hi)' }}>
                  {cat.nombreCategoria} ({cat.distancia} km)
                </h2>
                <Badge variant="neutral">
                  {filtering ? `${visible.length} de ${cat.resultados.length}` : `${cat.resultados.length} corredores`}
                </Badge>
              </div>

              {visible.length === 0 ? (
                <div className="py-6 text-center" data-od-id="no-results">
                  <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
                    {filtering
                      ? `Sin resultados para este filtro en ${cat.nombreCategoria}.`
                      : 'Sin resultados'}
                  </p>
                  {filtering && (
                    <button
                      className="mt-2 inline-block text-xs font-medium underline"
                      style={{ color: 'var(--accent)' }}
                      onClick={() => setQuery('')}
                    >
                      Limpiar filtro
                    </button>
                  )}
                </div>
              ) : (
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr style={{ color: 'var(--text-th)' }}>
                      <th className="py-1">Pos</th>
                      <th className="py-1">Dorsal</th>
                      <th className="py-1">Nombre</th>
                      <th className="py-1">Tiempo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visible.map((res) => (
                      <tr key={res.runnerId} style={{ borderTop: '1px solid var(--bd-row)', color: 'var(--text-hi)' }}>
                        <td className="py-1.5 font-mono tabular-nums">{res.posicion}</td>
                        <td className="py-1.5 font-mono tabular-nums">{res.dorsal}</td>
                        <td className="py-1.5">
                          <Link
                            to={`/corredor/${encodeShareKey(token ?? '', res.runnerId)}`}
                            className="font-medium no-underline"
                            style={{ color: 'var(--accent)' }}
                          >
                            {res.nombre}
                          </Link>
                        </td>
                        <td className="py-1.5 font-mono tabular-nums">{formatTime(res.tiempoLlegada)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          )
        })}
      </main>
    </div>
  )
}