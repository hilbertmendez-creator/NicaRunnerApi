import { useState, type ReactNode } from 'react'
import { Input } from './form/Input'

interface ToolbarProps {
  searchPlaceholder?: string
  onSearch?: (value: string) => void
  searchValue?: string
  children?: ReactNode
  className?: string
}

/** Barra de búsqueda + filtros/acciones (referencia Race-Day Control Room `.toolbar`). */
export function Toolbar({
  searchPlaceholder = 'Buscar...',
  onSearch,
  searchValue,
  children,
  className = '',
}: ToolbarProps) {
  const [internal, setInternal] = useState('')
  const value = searchValue ?? internal

  return (
    <div className={`flex flex-wrap items-center gap-3 ${className}`}>
      {onSearch && (
        <Input
          value={value}
          onChange={(e) => {
            const next = e.target.value
            setInternal(next)
            onSearch(next)
          }}
          placeholder={searchPlaceholder}
          className="max-w-xs"
          aria-label={searchPlaceholder}
        />
      )}
      {children}
    </div>
  )
}