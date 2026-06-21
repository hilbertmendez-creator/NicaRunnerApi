import { useEffect, useState } from 'react'
import { deleteRunner, getCategories, getRunners } from '../../api/endpoints'
import type { RaceCategoryDto, RunnerDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button } from '../../components/Button'
import { RunnerFormModal } from './RunnerFormModal'
import { ImportExcelModal } from './ImportExcelModal'

export function RunnersTab({ raceId }: { raceId: number }) {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [runners, setRunners] = useState<RunnerDto[]>([])
  const [categories, setCategories] = useState<RaceCategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<RunnerDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [showImport, setShowImport] = useState(false)

  function reload() {
    setLoading(true)
    Promise.all([getRunners(raceId), getCategories(raceId)])
      .then(([runnersData, categoriesData]) => {
        setRunners(runnersData)
        setCategories(categoriesData)
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  function categoryName(categoryId: number) {
    return categories.find((c) => c.id === categoryId)?.nombreCategoria ?? '—'
  }

  async function handleDelete(runner: RunnerDto) {
    if (!confirm(`¿Eliminar al corredor "${runner.nombre}" (dorsal ${runner.dorsal})?`)) return
    await deleteRunner(raceId, runner.id)
    reload()
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-end gap-2">
        {canManage && (
          <>
            <Button onClick={() => setShowImport(true)}>Importar Excel</Button>
            <Button
              variant="primary"
              onClick={() => setShowCreate(true)}
              disabled={categories.length === 0}
            >
              Nuevo corredor
            </Button>
          </>
        )}
      </div>

      {canManage && categories.length === 0 && (
        <p className="text-sm text-amber-700">
          Crea al menos una categoría antes de registrar corredores.
        </p>
      )}

      {loading && <p className="text-sm text-gray-500">Cargando corredores...</p>}

      {!loading && runners.length === 0 && (
        <p className="text-sm text-gray-500">Esta carrera no tiene corredores registrados todavía.</p>
      )}

      {runners.length > 0 && (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-gray-500">
              <th className="py-1">Dorsal</th>
              <th className="py-1">Nombre</th>
              <th className="py-1">Categoría</th>
              <th className="py-1">Edad</th>
              <th className="py-1">Contacto</th>
              <th className="py-1"></th>
            </tr>
          </thead>
          <tbody>
            {runners.map((runner) => (
              <tr key={runner.id} className="border-t border-gray-100">
                <td className="py-2">{runner.dorsal}</td>
                <td className="py-2">{runner.nombre}</td>
                <td className="py-2">{categoryName(runner.categoryId)}</td>
                <td className="py-2">{runner.edad}</td>
                <td className="py-2 text-gray-500">{runner.email ?? runner.telefono ?? '—'}</td>
                <td className="flex gap-2 py-2">
                  {canManage && (
                    <>
                      <Button size="sm" onClick={() => setEditing(runner)}>
                        Editar
                      </Button>
                      <Button size="sm" variant="destructive" onClick={() => handleDelete(runner)}>
                        Eliminar
                      </Button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {showCreate && (
        <RunnerFormModal
          raceId={raceId}
          runner={null}
          categories={categories}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <RunnerFormModal
          raceId={raceId}
          runner={editing}
          categories={categories}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}

      {showImport && (
        <ImportExcelModal
          raceId={raceId}
          onClose={() => setShowImport(false)}
          onImported={reload}
        />
      )}
    </div>
  )
}
