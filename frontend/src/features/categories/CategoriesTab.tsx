import { useEffect, useState } from 'react'
import { deleteCategory, getCategories } from '../../api/endpoints'
import type { RaceCategoryDto } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { CategoryFormModal } from './CategoryFormModal'

export function CategoriesTab({ raceId }: { raceId: number }) {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [categories, setCategories] = useState<RaceCategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<RaceCategoryDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  function reload() {
    setLoading(true)
    getCategories(raceId)
      .then(setCategories)
      .finally(() => setLoading(false))
  }

  useEffect(reload, [raceId])

  async function handleDelete(category: RaceCategoryDto) {
    if (!confirm(`¿Eliminar la categoría "${category.nombreCategoria}"?`)) return
    await deleteCategory(raceId, category.id)
    reload()
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-end">
        {canManage && (
          <button
            onClick={() => setShowCreate(true)}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            Nueva categoría
          </button>
        )}
      </div>

      {loading && <p className="text-sm text-gray-500">Cargando categorías...</p>}

      {!loading && categories.length === 0 && (
        <p className="text-sm text-gray-500">Esta carrera no tiene categorías todavía.</p>
      )}

      {categories.length > 0 && (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-gray-500">
              <th className="py-1">Nombre</th>
              <th className="py-1">Distancia</th>
              <th className="py-1">Edad</th>
              <th className="py-1">Orden</th>
              <th className="py-1"></th>
            </tr>
          </thead>
          <tbody>
            {categories
              .sort((a, b) => a.orden - b.orden)
              .map((cat) => (
                <tr key={cat.id} className="border-t border-gray-100">
                  <td className="py-2">{cat.nombreCategoria}</td>
                  <td className="py-2">{cat.distancia} km</td>
                  <td className="py-2">
                    {cat.edadMinima}–{cat.edadMaxima}
                  </td>
                  <td className="py-2">{cat.orden}</td>
                  <td className="flex gap-2 py-2">
                    {canManage && (
                      <>
                        <button
                          onClick={() => setEditing(cat)}
                          className="rounded border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-100"
                        >
                          Editar
                        </button>
                        <button
                          onClick={() => handleDelete(cat)}
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
      )}

      {showCreate && (
        <CategoryFormModal
          raceId={raceId}
          category={null}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <CategoryFormModal
          raceId={raceId}
          category={editing}
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
