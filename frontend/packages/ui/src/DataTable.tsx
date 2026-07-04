import type { ReactNode } from 'react'

export interface Column<T> {
  header: string
  render: (row: T) => ReactNode
  className?: string
}

interface DataTableProps<T> {
  columns: Column<T>[]
  data: T[]
  rowKey: (row: T) => string | number
  emptyState?: ReactNode
}

export function DataTable<T>({ columns, data, rowKey, emptyState }: DataTableProps<T>) {
  if (data.length === 0 && emptyState) {
    return <>{emptyState}</>
  }

  return (
    <div
      className="overflow-x-auto"
      style={{ border: '1px solid var(--bd-card)', background: 'var(--bg-card)' }}
    >
      <table className="w-full border-collapse text-left text-sm">
        <thead>
          <tr
            className="text-xs uppercase tracking-wide"
            style={{ borderBottom: '1px solid var(--bd)', background: 'var(--bg-th)', color: 'var(--text-th)' }}
          >
            {columns.map((col, idx) => (
              <th key={idx} className={`h-8 px-3 font-medium ${col.className ?? ''}`}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row) => (
            <tr
              key={rowKey(row)}
              className="row-hover h-9"
              style={{ borderBottom: '1px solid var(--bd-row)', color: 'var(--text-hi)' }}
            >
              {columns.map((col, idx) => (
                <td key={idx} className={`px-3 align-middle ${col.className ?? ''}`}>
                  {col.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
