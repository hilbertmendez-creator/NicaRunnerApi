import { useEffect, useState } from 'react'
import { deleteRunner, getCategories, getRunners } from '../../api/endpoints'
import type { RaceCategoryDto, RunnerDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, ErrorAlert } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
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

  const columns: Column<RunnerDto>[] = [
    {
      header: 'Dorsal',
      render: (runner) => runner.dorsal,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Nombre',
      render: (runner) => runner.nombre,
    },
    {
      header: 'Categoría',
      render: (runner) => categoryName(runner.categoryId),
    },
    {
      header: 'Edad',
      render: (runner) => runner.edad,
      className: 'font-mono tabular-nums',
    },
    {
      header: 'Contacto',
      render: (runner) => (
        <span className="text-zinc-500">{runner.email ?? runner.telefono ?? '—'}</span>
      ),
    },
    {
      header: '',
      render: (runner) => (
        <div className="flex gap-2">
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
        </div>
      ),
    },
  ]

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
        <ErrorAlert message="Crea al menos una categoría antes de registrar corredores." />
      )}

      {loading && <LoadingText message="Cargando corredores..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={runners}
          rowKey={(runner) => runner.id}
          emptyState={<EmptyState message="Esta carrera no tiene corredores registrados todavía." />}
        />
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
