interface LoadingTextProps {
  message?: string
  className?: string
}

export function LoadingText({ message = 'Cargando...', className = '' }: LoadingTextProps) {
  return (
    <div role="status" aria-live="polite" className={`flex items-center gap-3 py-4 ${className}`}>
      <div
        className="nr-spinner h-4 w-4 rounded-full border-2 border-t-transparent"
        style={{ borderColor: 'var(--ac, #2563EB)', borderTopColor: 'transparent' }}
        aria-hidden="true"
      />
      <span className="text-sm font-medium" style={{ color: 'var(--text-lo, #71717a)' }}>
        {message}
      </span>
    </div>
  )
}
