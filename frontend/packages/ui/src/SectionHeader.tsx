import type { ReactNode } from 'react'

interface SectionHeaderProps {
  title: string
  subtitle?: string
  actions?: ReactNode
  className?: string
}

/** Encabezado de pantalla/sección (referencia Race-Day Control Room `.section-header`). */
export function SectionHeader({ title, subtitle, actions, className = '' }: SectionHeaderProps) {
  return (
    <div className={`mb-4 flex flex-wrap items-center justify-between gap-3 ${className}`}>
      <div>
        <h2 className="text-lg font-semibold text-zinc-900">{title}</h2>
        {subtitle && <p className="text-sm text-zinc-500">{subtitle}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  )
}