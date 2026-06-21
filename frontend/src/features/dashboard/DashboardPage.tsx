import { useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
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
    <div className="flex flex-col gap-6">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold text-gray-900">Dashboard en vivo</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      {!raceId && <p className="text-sm text-gray-500">Selecciona una carrera para ver su progreso.</p>}

      {raceId && dashboard.loading && !dashboard.data && (
        <p className="text-sm text-gray-500">Cargando dashboard...</p>
      )}

      {dashboard.data && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <SummaryCard label="Inscritos" value={dashboard.data.totalInscritos} />
            <SummaryCard label="Con tiempo" value={dashboard.data.totalConTiempo} />
            <SummaryCard label="Pendientes" value={dashboard.data.totalPendientes} />
          </div>

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
              </tbody>
            </table>
          </section>
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

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg bg-white p-4 shadow-sm">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="text-2xl font-semibold text-gray-900">{value}</p>
    </div>
  )
}
