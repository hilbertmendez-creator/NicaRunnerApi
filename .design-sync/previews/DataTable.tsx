import { DataTable, EmptyState } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'

type Runner = { id: number; position: number; number: number; name: string; category: string; finishTime: string }

const COLUMNS: Column<Runner>[] = [
  { header: 'Pos.', render: (r) => r.position, className: 'font-mono tabular-nums w-12' },
  { header: 'Dorsal', render: (r) => r.number, className: 'font-mono tabular-nums w-16' },
  { header: 'Nombre', render: (r) => r.name },
  { header: 'Categoría', render: (r) => r.category },
  { header: 'Tiempo', render: (r) => r.finishTime, className: 'font-mono tabular-nums' },
]

const RUNNERS: Runner[] = [
  { id: 1, position: 1, number: 101, name: 'Carlos Martínez', category: 'Varonil Mayor', finishTime: '02:14:38' },
  { id: 2, position: 2, number: 215, name: 'Pedro Gómez', category: 'Varonil Mayor', finishTime: '02:19:55' },
  { id: 3, position: 3, number: 88, name: 'Luis Herrera', category: 'Varonil Mayor', finishTime: '02:23:11' },
  { id: 4, position: 1, number: 305, name: 'Ana López', category: 'Femenil Mayor', finishTime: '02:31:47' },
  { id: 5, position: 2, number: 412, name: 'María Torres', category: 'Femenil Mayor', finishTime: '02:38:09' },
]

export function ResultsTable() {
  return (
    <div className="p-4">
      <DataTable columns={COLUMNS} data={RUNNERS} rowKey={(r) => r.id} />
    </div>
  )
}

export function EmptyTable() {
  return (
    <div className="p-4">
      <DataTable
        columns={COLUMNS}
        data={[]}
        rowKey={(r) => r.id}
        emptyState={<EmptyState message="Sin resultados capturados todavía." />}
      />
    </div>
  )
}
