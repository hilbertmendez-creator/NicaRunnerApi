interface EmptyStateProps {
  message: string
  className?: string
}

export function EmptyState({ message, className = '' }: EmptyStateProps) {
  return (
    <div
      className={`flex flex-col items-center justify-center border border-dashed p-8 text-center ${className}`}
      style={{ borderColor: 'var(--bd)', background: 'var(--bg-hover)' }}
    >
      <p className="text-sm font-medium" style={{ color: 'var(--text-lo)' }}>{message}</p>
    </div>
  )
}
