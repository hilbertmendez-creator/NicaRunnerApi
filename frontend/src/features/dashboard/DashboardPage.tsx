import { useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { StatusBadge } from '../../components/StatusBadge'
import { getDashboard, getStandings } from '../../api/endpoints'
import { usePolling } from '../../hooks/usePolling'

const POLL_INTERVAL_MS = 5000

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

export function DashboardPage() {
  const [raceId, setRaceId] = useState<number | null>(null)

  const dashboard = usePolling(
    () => (raceId ? getDashboard(raceId) : Promise.resolve(null)),
    POLL_INTERVAL_MS,
    [raceId],
  )
  const standings = usePolling(
    () => (raceId ? getStandings(raceId) : Promise.resolve([])),
    POLL_INTERVAL_MS,
    [raceId],
  )

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-lg font-semibold text-gray-900">
            {dashboard.data?.raceName ?? 'Dashboard en vivo'}
          </h1>
          {dashboard.data && <StatusBadge status={dashboard.data.estado} />}
        </div>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      {!raceId && <p className="text-sm text-gray-500">Selecciona una carrera para ver su progreso.</p>}

      {raceId && dashboard.loading && !dashboard.data && (
        <p className="text-sm text-gray-500">Cargando dashboard...</p>
      )}

      {dashboard.data && (
        <>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="rounded-lg bg-orange-50 p-4">
              <p className="text-sm text-orange-700">Inscritos</p>
              <p className="text-2xl font-medium text-orange-900">{dashboard.data.totalInscritos}</p>
            </div>
            <div className="rounded-lg bg-teal-50 p-4">
              <p className="text-sm text-teal-700">Con tiempo</p>
              <p className="text-2xl font-medium text-teal-900">{dashboard.data.totalConTiempo}</p>
            </div>
            <div className="rounded-lg bg-amber-50 p-4">
              <p className="text-sm text-amber-700">Pendientes</p>
              <p className="text-2xl font-medium text-amber-900">{dashboard.data.totalPendientes}</p>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1.3fr_1fr]">
            <section className="rounded-lg bg-white p-4 shadow-sm">
              <h2 className="mb-3 text-sm font-semibold text-gray-900">Últimos resultados capturados</h2>
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="text-gray-500">
                    <th className="py-1">Dorsal</th>
                    <th className="py-1">Nombre</th>
                    <th className="py-1">Categoría</th>
                    <th className="py-1">Posición</th>
                    <th className="py-1">Hora</th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.data.ultimosResultados.map((r) => (
                    <tr key={r.resultId} className="border-t border-gray-100">
                      <td className="py-1.5">{r.dorsal}</td>
                      <td className="py-1.5">{r.nombre}</td>
                      <td className="py-1.5">{r.nombreCategoria}</td>
                      <td className="py-1.5">{r.posicion}</td>
                      <td className="py-1.5">{formatTime(r.tiempoLlegada)}</td>
                    </tr>
                  ))}
                  {dashboard.data.ultimosResultados.length === 0 && (
                    <tr>
                      <td colSpan={5} className="py-3 text-center text-gray-400">
                        Sin resultados capturados todavía.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </section>

            <section className="rounded-lg bg-white p-4 shadow-sm">
              <h2 className="mb-3 text-sm font-semibold text-gray-900">Progreso por categoría</h2>
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="text-gray-500">
                    <th className="py-1">Categoría</th>
                    <th className="py-1">Inscritos</th>
                    <th className="py-1">Con tiempo</th>
                    <th className="py-1">Pendientes</th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.data.categorias.map((cat) => (
                    <tr key={cat.categoryId} className="border-t border-gray-100">
                      <td className="py-1.5">{cat.nombreCategoria}</td>
                      <td className="py-1.5">{cat.inscritos}</td>
                      <td className="py-1.5">{cat.conTiempo}</td>
                      <td className="py-1.5">{cat.pendientes}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          </div>
        </>
      )}

      {standings.data && standings.data.length > 0 && (
        <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {standings.data.map((cat) => (
            <div key={cat.categoryId} className="rounded-lg bg-white p-4 shadow-sm">
              <h3 className="mb-2 text-sm font-semibold text-gray-900">
                {cat.nombreCategoria} ({cat.distancia} km)
              </h3>
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="text-gray-500">
                    <th className="py-1">Pos.</th>
                    <th className="py-1">Dorsal</th>
                    <th className="py-1">Nombre</th>
                    <th className="py-1">Hora</th>
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
            </div>
          ))}
        </section>
      )}
    </div>
  )
}
