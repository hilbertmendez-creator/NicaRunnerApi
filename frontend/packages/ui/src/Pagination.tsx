import { Button } from './Button'

interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
  className?: string
}

/** Paginación de tablas (referencia Race-Day Control Room).
 *  Muestra el rango mostrado y botones anterior/siguiente. */
export function Pagination({ page, pageSize, total, onChange, className = '' }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1
  const end = total === 0 ? 0 : Math.min(page * pageSize, total)

  return (
    <div className={`flex items-center justify-between px-4 py-3 ${className}`}>
      <span className="text-sm text-zinc-500">
        Mostrando {start}–{end} de {total}
      </span>
      <div className="flex gap-2">
        <Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          Anterior
        </Button>
        <span className="flex items-center px-2 text-sm font-medium text-zinc-700">
          {page} / {totalPages}
        </span>
        <Button
          size="sm"
          variant="secondary"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
        >
          Siguiente
        </Button>
      </div>
    </div>
  )
}