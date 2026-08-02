import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '@nicarunner/ui'
import { getPublicResults } from '../../api/endpoints'
import type { PublicRaceResultsDto } from '../../api/types'

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

export function PublicResultsPage() {
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const [data, setData] = useState<PublicRaceResultsDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [reloadKey, setReloadKey] = useState(0)

  const retry = useCallback(() => {
    setError(null)
    setData(null)
    setReloadKey((k) => k + 1)
  }, [])

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
  }, [token, reloadKey])

  return (
    <div className="min-h-screen" style={{ background: 'var(--bg-app)' }}>
      <header
        className="px-6 py-3"
        style={{ background: 'var(--bg-topbar)', borderBottom: '1px solid var(--bd)' }}
      >
        <span className="text-base font-semibold" style={{ color: 'var(--accent)' }}>nicaRunner</span>
        <span className="ml-2 text-sm" style={{ color: 'var(--text-xs)' }}>Resultados</span>
      </header>

      <main className="mx-auto max-w-3xl p-6">
        {loading && <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando resultados...</p>}

        {error && <ErrorAlert message={error} onRetry={retry} />}

        {data && (
          <div className="flex flex-col gap-5">
            <div className="min-w-0">
              <h1
                className="text-lg font-semibold"
                style={{ color: 'var(--text-hi)', overflowWrap: 'anywhere' }}
              >
                {data.raceName}
              </h1>
              <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
                {new Date(data.fechaCarrera).toLocaleDateString('es-NI')}
              </p>
            </div>

            {data.categorias.length === 0 && (
              <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Todavía no hay resultados publicados.</p>
            )}

            {data.categorias.map((cat) => (
              <section
                key={cat.nombreCategoria}
                className="min-w-0 p-4"
                style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}
              >
                <h2
                  className="mb-3 text-sm font-semibold"
                  style={{ color: 'var(--text-hi)', overflowWrap: 'anywhere' }}
                >
                  {cat.nombreCategoria} ({cat.distancia} km)
                </h2>
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead>
                      <tr style={{ color: 'var(--text-th)' }}>
                        <th className="py-1">Pos.</th>
                        <th className="py-1">Dorsal</th>
                        <th className="py-1">Nombre</th>
                        <th className="py-1">Tiempo</th>
                      </tr>
                    </thead>
                    <tbody>
                      {cat.resultados.map((res) => {
                        // spec.md "Conditional Return-to-Results Link": el token viaja por
                        // router state, nunca en la URL — un visitante que abre el enlace
                        // compartido directamente (sin state) nunca ve el link de regreso.
                        // Filas sin ShareKey (backfill pendiente) quedan no-clicables, pero
                        // deben explicarlo: de lo contrario un corredor que hace clic en su
                        // propia fila no entiende por qué no pasa nada.
                        const clickable = res.shareKey !== null
                        const explicacionSinEnlace = 'El enlace individual de este corredor todavía no está disponible.'
                        return (
                          <tr
                            key={res.runnerId}
                            onClick={clickable
                              ? () => navigate(`/corredor/${res.shareKey}`, { state: { fromToken: token } })
                              : undefined}
                            title={clickable ? undefined : explicacionSinEnlace}
                            style={{
                              borderTop: '1px solid var(--bd-row)',
                              color: clickable ? 'var(--text-hi)' : 'var(--text-lo)',
                              cursor: clickable ? 'pointer' : 'default',
                            }}
                          >
                            <td className="py-1.5">{res.posicion}</td>
                            <td className="py-1.5 font-mono tabular-nums">{res.dorsal}</td>
                            <td className="max-w-[14rem] py-1.5" style={{ overflowWrap: 'anywhere' }}>
                              {res.nombre}
                              {!clickable && (
                                <span className="ml-2 text-xs" style={{ color: 'var(--text-lo)' }}>
                                  (enlace no disponible aún)
                                </span>
                              )}
                            </td>
                            <td className="py-1.5 font-mono tabular-nums">{formatTime(res.tiempoLlegada)}</td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              </section>
            ))}
          </div>
        )}
      </main>
    </div>
  )
}
