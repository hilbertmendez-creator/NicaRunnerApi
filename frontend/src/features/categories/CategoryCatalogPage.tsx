import { useEffect, useState } from 'react'
import { deleteCategoryCatalogEntry, getCategoryAudit, getCategoryCatalog } from '../../api/endpoints'
import type { CategoryDto } from '../../api/types'
import { Button, DataTable, LoadingText, EmptyState, SectionHeader } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { CategoryCatalogFormModal } from './CategoryCatalogFormModal'
import { EntityAuditHistory } from '../../components/EntityAuditHistory'

export function CategoryCatalogPage() {
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<CategoryDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [auditingCategory, setAuditingCategory] = useState<CategoryDto | null>(null)

  function reload() {
    setLoading(true)
    getCategoryCatalog()
      .then(setCategories)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [])

  async function handleDelete(category: CategoryDto) {
    if (!confirm(`¿Eliminar la categoría "${category.nombreCategoria}"?`)) return
    await deleteCategoryCatalogEntry(category.id)
    reload()
  }

  const columns: Column<CategoryDto>[] = [
    { header: 'Orden', render: (cat) => cat.orden, className: 'font-mono tabular-nums' },
    { header: 'Código', render: (cat) => cat.codigo, className: 'font-mono' },
    { header: 'Nombre', render: (cat) => cat.nombreCategoria },
    { header: 'Descripción', render: (cat) => cat.descripcion ?? '—' },
    {
      header: 'Distancia',
      render: (cat) => `${cat.distancia} km`,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Rango edad',
      render: (cat) => `${cat.edadMinima}–${cat.edadMaxima}`,
      className: 'font-mono tabular-nums',
    },
    {
      header: '',
      render: (cat) => (
        <div className="flex gap-2">
          <Button size="sm" onClick={() => setEditing(cat)}>
            Editar
          </Button>
          <Button size="sm" onClick={() => setAuditingCategory(cat)}>
            Historial
          </Button>
          <Button size="sm" variant="destructive" onClick={() => handleDelete(cat)}>
            Eliminar
          </Button>
        </div>
      ),
    },
  ]

  const sortedCategories = [...categories].sort((a, b) => a.orden - b.orden)

  return (
    <div className="flex flex-col gap-3">
      <SectionHeader
        title="Categorías del sistema"
        subtitle={`${sortedCategories.length} categorías ordenadas por código · usadas como catálogo maestro para todas las carreras`}
        actions={
          <Button variant="primary" onClick={() => setShowCreate(true)}>
            + Nueva categoría
          </Button>
        }
      />

      {loading && <LoadingText message="Cargando categorías..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={sortedCategories}
          rowKey={(cat) => cat.id}
          emptyState={<EmptyState message="Todavía no hay categorías en el catálogo." />}
        />
      )}

      {showCreate && (
        <CategoryCatalogFormModal
          category={null}
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}

      {editing && (
        <CategoryCatalogFormModal
          category={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}

      {auditingCategory && (
        <EntityAuditHistory
          title={`Auditoría — categoría "${auditingCategory.nombreCategoria}"`}
          load={() => getCategoryAudit(auditingCategory.id)}
          onClose={() => setAuditingCategory(null)}
        />
      )}
    </div>
  )
}
