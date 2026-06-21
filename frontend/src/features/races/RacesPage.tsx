import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { deleteRace, getRaces } from '../../api/endpoints'
import type { RaceDto } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { StatusBadge } from '../../components/StatusBadge'
import { RaceFormModal } from './RaceFormModal'

export function RacesPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [races, setRaces] = useState<RaceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<RaceDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  function reload() {
    setLoading(true)
    getRaces()
      .then(setRaces)
      .finally(() => setLoading(false))
  }

  useEffect(reload, [])

  async function handleDelete(race: RaceDto) {
    if (!confirm(`¿Eliminar la carrera "${race.nombre}"? Esta acción no se puede deshacer.`)) return
    await deleteRace(race.id)
    reload()
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-gray-900">Carreras</h1>
        {canManage && (
          <button
            onClick={() => setShowCreate(true)}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            Nueva carrera
          </button>
        )}
      </div>

      {loading && <p className="text-sm text-gray-500">Cargando carreras...</p>}

      {!loading && races.length === 0 && (
        <p className="text-sm text-gray-500">No hay carreras creadas todavía.</p>
      )}

      {races.length > 0 && (
        <section className="overflow-x-auto rounded-lg bg-white p-4 shadow-sm">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-gray-500">
                <th className="py-1">Nombre</th>
                <th className="py-1">Fecha</th>
                <th className="py-1">Estado</th>
                <th className="py-1"></th>
              </tr>
            </thead>
            <tbody>
              {races.map((race) => (
                <tr key={race.id} className="border-t border-gray-100">
                  <td className="py-2">
                    <Link to={`/carreras/${race.id}`} className="font-medium text-blue-700 hover:underline">
                      {race.nombre}
                    </Link>
                  </td>
                  <td className="py-2">{new Date(race.fechaCarrera).toLocaleDateString('es-NI')}</td>
                  <td className="py-2">
                    <StatusBadge status={race.estado} />
                  </td>
                  <td className="flex gap-2 py-2">
                    {canManage && (
                      <>
                        <button
                          onClick={() => setEditing(race)}
                          className="rounded border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-100"
                        >
                          Editar
                        </button>
                        <button
                          onClick={() => handleDelete(race)}
                          className="rounded border border-red-300 px-2 py-1 text-xs text-red-700 hover:bg-red-50"
                        >
                          Eliminar
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {showCreate && (
        <RaceFormModal
          race={null}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <RaceFormModal
          race={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}
    </div>
  )
}
