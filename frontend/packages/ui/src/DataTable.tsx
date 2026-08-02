import type { ReactNode } from 'react'
import { LoadingText } from './LoadingText'

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
  isLoading?: boolean
  loadingMessage?: string
}

export function DataTable<T>({
  columns,
  data,
  rowKey,
  emptyState,
  isLoading = false,
  loadingMessage,
}: DataTableProps<T>) {
  if (isLoading) {
    return <LoadingText message={loadingMessage} />
  }

  if (data.length === 0 && emptyState) {
    return <>{emptyState}</>
  }

  const labeledColumns = columns.filter((col) => col.header !== '')
  const actionColumns = columns.filter((col) => col.header === '')

  return (
    <>
      {/* Tarjetas — solo pantallas angostas (<sm) */}
      <div className="flex flex-col gap-2 sm:hidden">
        {data.map((row) => (
          <div
            key={rowKey(row)}
            className="p-3"
            style={{
              background: 'var(--bg-card, #ffffff)',
              border: '1px solid var(--bd-card, #e4e4e7)',
                  borderRadius: 'var(--radius-card, 7px)',
            }}
          >
            {labeledColumns.map((col, idx) => (
              <div key={idx} className="flex items-start justify-between gap-3 py-1 text-sm">
                <span className="text-xs uppercase tracking-wide" style={{ color: 'var(--text-th, #71717a)' }}>
                  {col.header}
                </span>
                <span className={`text-right ${col.className ?? ''}`} style={{ color: 'var(--text-hi, #18181b)' }}>
                  {col.render(row)}
                </span>
              </div>
            ))}
            {actionColumns.length > 0 && (
              <div className="mt-2 flex justify-end gap-2 border-t pt-2" style={{ borderColor: 'var(--bd-row, #e4e4e7)' }}>
                {actionColumns.map((col, idx) => (
                  <div key={idx}>{col.render(row)}</div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Tabla — sm y mayores */}
      <div
        className="hidden overflow-x-auto sm:block"
        style={{
          border: '1px solid var(--bd-card, #e4e4e7)',
          background: 'var(--bg-card, #ffffff)',
          borderRadius: 'var(--radius-card, 7px)',
        }}
      >
        <table className="w-full border-collapse text-left text-sm">
          <thead className="sticky top-0">
            <tr
              className="text-xs uppercase tracking-wide"
              style={{
                borderBottom: '1px solid var(--bd, #e4e4e7)',
                background: 'var(--bg-th, #fafafa)',
                color: 'var(--text-th, #71717a)',
              }}
            >
              {columns.map((col, idx) => (
                <th key={idx} scope="col" className={`h-8 px-3 font-medium ${col.className ?? ''}`}>
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
                style={{ borderBottom: '1px solid var(--bd-row, #e4e4e7)', color: 'var(--text-hi, #18181b)' }}
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
    </>
  )
}
