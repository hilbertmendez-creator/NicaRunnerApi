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
    <div className="min-h-screen bg-gray-100">
      <header className="border-b border-gray-200 bg-white px-6 py-3">
        <span className="text-base font-semibold text-orange-700">nicaRunner</span>
        <span className="ml-2 text-sm text-gray-400">Resultados</span>
      </header>

      <main className="mx-auto max-w-3xl p-6">
        {loading && <p className="text-sm text-gray-500">Cargando resultados...</p>}

        {error && (
          <div className="rounded-lg bg-white p-6 text-center shadow-sm">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        {data && (
          <div className="flex flex-col gap-5">
            <div>
              <h1 className="text-lg font-semibold text-gray-900">{data.raceName}</h1>
              <p className="text-sm text-gray-500">
                {new Date(data.fechaCarrera).toLocaleDateString('es-NI')}
              </p>
            </div>

            {data.categorias.length === 0 && (
              <p className="text-sm text-gray-500">Todavía no hay resultados publicados.</p>
            )}

            {data.categorias.map((cat) => (
              <section key={cat.nombreCategoria} className="rounded-lg bg-white p-4 shadow-sm">
                <h2 className="mb-3 text-sm font-semibold text-gray-900">
                  {cat.nombreCategoria} ({cat.distancia} km)
                </h2>
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="text-gray-500">
                      <th className="py-1">Pos.</th>
                      <th className="py-1">Dorsal</th>
                      <th className="py-1">Nombre</th>
                      <th className="py-1">Tiempo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {cat.resultados.map((res) => (
                      <tr key={res.runnerId} className="border-t border-gray-100">
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
