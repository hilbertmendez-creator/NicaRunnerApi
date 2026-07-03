import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getPublicResults } from '../../api/endpoints'
import type { PublicRaceResultsDto } from '../../api/types'

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

export function PublicResultsPage() {
  const { token } = useParams<{ token: string }>()
  const [data, setData] = useState<PublicRaceResultsDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

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
      <header
        className="px-6 py-3"
        style={{ background: 'var(--bg-topbar)', borderBottom: '1px solid var(--bd)' }}
      >
        <span className="text-base font-semibold" style={{ color: 'var(--accent)' }}>nicaRunner</span>
        <span className="ml-2 text-sm" style={{ color: 'var(--text-xs)' }}>Resultados</span>
      </header>

      <main className="mx-auto max-w-3xl p-6">
        {loading && <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Cargando resultados...</p>}

        {error && (
          <div className="p-6 text-center" style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}>
            <p className="text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>
          </div>
        )}

        {data && (
          <div className="flex flex-col gap-5">
            <div>
              <h1 className="text-lg font-semibold" style={{ color: 'var(--text-hi)' }}>{data.raceName}</h1>
              <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
                {new Date(data.fechaCarrera).toLocaleDateString('es-NI')}
              </p>
            </div>

            {data.categorias.length === 0 && (
              <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Todavía no hay resultados publicados.</p>
            )}

            {data.categorias.map((cat) => (
              <section key={cat.nombreCategoria} className="p-4" style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}>
                <h2 className="mb-3 text-sm font-semibold" style={{ color: 'var(--text-hi)' }}>
                  {cat.nombreCategoria} ({cat.distancia} km)
                </h2>
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
                    {cat.resultados.map((res) => (
                      <tr key={res.runnerId} style={{ borderTop: '1px solid var(--bd-row)', color: 'var(--text-hi)' }}>
                        <td className="py-1.5">{res.posicion}</td>
                        <td className="py-1.5">{res.dorsal}</td>
                        <td className="py-1.5">{res.nombre}</td>
                        <td className="py-1.5">{formatTime(res.tiempoLlegada)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </section>
            ))}
          </div>
        )}
      </main>
    </div>
  )
}
