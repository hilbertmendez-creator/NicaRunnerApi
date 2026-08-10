import { useEffect, useState } from 'react'
import { assignCategory, getCategories, getCategoryCatalog, unassignCategory } from '../../api/endpoints'
import type { CategoryDto, RaceCategoryDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, Select } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'

export function CategoriesTab({ raceId }: { raceId: number }) {
  const { user } = useAuth()
  const canManage = user?.role === 'Administrador'

  const [categories, setCategories] = useState<RaceCategoryDto[]>([])
  const [catalog, setCatalog] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null)
  const [assigning, setAssigning] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function reload() {
    setLoading(true)
    Promise.all([getCategories(raceId), getCategoryCatalog()])
      .then(([assigned, all]) => {
        setCategories(assigned)
        setCatalog(all)
      })
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  const availableToAssign = catalog
    .filter((cat) => !categories.some((assigned) => assigned.categoryId === cat.id))
    .sort((a, b) => a.orden - b.orden)

  async function handleAssign() {
    if (selectedCategoryId === null) return
    setError(null)
    setAssigning(true)
    try {
      await assignCategory(raceId, { categoryId: selectedCategoryId })
      setSelectedCategoryId(null)
      reload()
    } catch (err: any) {
      setError(err.response?.data?.detail ?? 'No se pudo agregar la categoría.')
    } finally {
      setAssigning(false)
    }
  }

  async function handleUnassign(category: RaceCategoryDto) {
    if (!confirm(`¿Quitar la categoría "${category.nombreCategoria}" de esta carrera?`)) return
    setError(null)
    try {
      await unassignCategory(raceId, category.categoryId)
      reload()
    } catch (err: any) {
      setError(err.response?.data?.detail ?? 'No se pudo quitar la categoría.')
    }
  }

  const columns: Column<RaceCategoryDto>[] = [
    { header: 'Código', render: (cat) => cat.codigo, className: 'font-mono' },
    { header: 'Nombre', render: (cat) => cat.nombreCategoria },
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
      header: '',
      render: (cat) => (
        <div className="flex gap-2">
          {canManage && (
            <Button size="sm" variant="destructive" onClick={() => handleUnassign(cat)}>
              Quitar
            </Button>
          )}
        </div>
      ),
    },
  ]

  const sortedCategories = [...categories].sort((a, b) => a.orden - b.orden)

  return (
    <div className="flex flex-col gap-3">
      {canManage && (
        <div className="flex items-center justify-end gap-2">
          <Select
            value={selectedCategoryId ?? ''}
            onChange={(e) => setSelectedCategoryId(e.target.value ? Number(e.target.value) : null)}
            disabled={availableToAssign.length === 0}
            aria-label="Categoría a asignar"
          >
            <option value="">Seleccioná una categoría del catálogo…</option>
            {availableToAssign.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {cat.codigo} — {cat.nombreCategoria}
              </option>
            ))}
          </Select>
          <Button variant="primary" onClick={handleAssign} disabled={selectedCategoryId === null || assigning}>
            {assigning ? 'Agregando...' : 'Agregar a la carrera'}
          </Button>
        </div>
      )}

      {error && (
        <p className="text-sm" role="alert" style={{ color: 'var(--badge-er-text)' }}>
          {error}
        </p>
      )}

      {loading && <LoadingText message="Cargando categorías..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={sortedCategories}
          rowKey={(cat) => cat.categoryId}
          emptyState={<EmptyState message="Esta carrera no tiene categorías asignadas todavía." />}
        />
      )}
    </div>
  )
}
