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

  // Pagination (optional — omit for small static tables).
  // pageIndex is 1-based (first page is 1). Callers must NOT pass 0-based indexes;
  // API offset = (pageIndex - 1) * pageSize.
  pageIndex?: number
  pageCount?: number
  onPageChange?: (page: number) => void
}

export function DataTable<T>({
  columns,
  data,
  rowKey,
  emptyState,
  isLoading = false,
  loadingMessage,
  pageIndex,
  pageCount,
  onPageChange,
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

      {/* Paginación */}
      {pageIndex !== undefined && pageCount !== undefined && onPageChange && pageCount > 1 && (
        <div className="mt-4 flex items-center justify-between border-t border-gray-200 bg-white px-4 py-3 sm:px-6 rounded-b-md" style={{ borderColor: 'var(--bd-card, #e4e4e7)' }}>
          <div className="flex flex-1 justify-between sm:hidden">
            <button
              onClick={() => onPageChange(Math.max(1, pageIndex - 1))}
              disabled={pageIndex === 1}
              className="relative inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
            >
              Anterior
            </button>
            <button
              onClick={() => onPageChange(Math.min(pageCount, pageIndex + 1))}
              disabled={pageIndex === pageCount}
              className="relative ml-3 inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
            >
              Siguiente
            </button>
          </div>
          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-gray-700">
                Página <span className="font-medium">{pageIndex}</span> de <span className="font-medium">{pageCount}</span>
              </p>
            </div>
            <div>
              <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                <button
                  onClick={() => onPageChange(Math.max(1, pageIndex - 1))}
                  disabled={pageIndex === 1}
                  className="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50"
                >
                  <span className="sr-only">Anterior</span>
                  &larr;
                </button>
                {/* Simplified page buttons for UX */}
                {Array.from({ length: pageCount }, (_, i) => i + 1).map((p) => {
                  // Only show current, first, last, and immediate neighbors
                  if (p === 1 || p === pageCount || Math.abs(p - pageIndex) <= 1) {
                    return (
                      <button
                        key={p}
                        onClick={() => onPageChange(p)}
                        className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                          p === pageIndex
                            ? 'z-10 bg-blue-600 text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600'
                            : 'text-gray-900 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0'
                        }`}
                      >
                        {p}
                      </button>
                    )
                  }
                  if (p === 2 && pageIndex > 3) return <span key={p} className="relative inline-flex items-center px-4 py-2 text-sm font-semibold text-gray-700 ring-1 ring-inset ring-gray-300">...</span>
                  if (p === pageCount - 1 && pageIndex < pageCount - 2) return <span key={p} className="relative inline-flex items-center px-4 py-2 text-sm font-semibold text-gray-700 ring-1 ring-inset ring-gray-300">...</span>
                  return null;
                })}
                <button
                  onClick={() => onPageChange(Math.min(pageCount, pageIndex + 1))}
                  disabled={pageIndex === pageCount}
                  className="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50"
                >
                  <span className="sr-only">Siguiente</span>
                  &rarr;
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
