interface LoadingTextProps {
  message?: string
  className?: string
}

export function LoadingText({ message = 'Cargando...', className = '' }: LoadingTextProps) {
  return (
    <div className={`flex items-center gap-3 py-4 ${className}`}>
      <div
        className="h-4 w-4 animate-spin rounded-full border-2 border-t-transparent"
        style={{ borderColor: 'var(--accent)', borderTopColor: 'transparent' }}
      />
      <span className="animate-pulse text-sm font-medium" style={{ color: 'var(--text-lo)' }}>{message}</span>
    </div>
  )
}
