import { useEffect, useState } from 'react'
import { deleteCategory, getCategories } from '../../api/endpoints'
import type { RaceCategoryDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
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

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  async function handleDelete(category: RaceCategoryDto) {
    if (!confirm(`¿Eliminar la categoría "${category.nombreCategoria}"?`)) return
    await deleteCategory(raceId, category.id)
    reload()
  }

  const columns: Column<RaceCategoryDto>[] = [
    {
      header: 'Nombre',
      render: (cat) => cat.nombreCategoria,
    },
    {
      header: 'Distancia',
      render: (cat) => `${cat.distancia} km`,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Edad',
      render: (cat) => `${cat.edadMinima}–${cat.edadMaxima}`,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Orden',
      render: (cat) => cat.orden,
      className: 'font-mono tabular-nums',
    },
    {
      header: '',
      render: (cat) => (
        <div className="flex gap-2">
          {canManage && (
            <>
              <Button size="sm" onClick={() => setEditing(cat)}>
                Editar
              </Button>
              <Button size="sm" variant="destructive" onClick={() => handleDelete(cat)}>
                Eliminar
              </Button>
            </>
          )}
        </div>
      ),
    },
  ]

  const sortedCategories = [...categories].sort((a, b) => a.orden - b.orden)

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-end">
        {canManage && (
          <Button variant="primary" onClick={() => setShowCreate(true)}>
            Nueva categoría
          </Button>
        )}
      </div>

      {loading && <LoadingText message="Cargando categorías..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={sortedCategories}
          rowKey={(cat) => cat.id}
          emptyState={<EmptyState message="Esta carrera no tiene categorías todavía." />}
        />
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
