interface LoadingTextProps {
  message?: string
  className?: string
}

export function LoadingText({ message = 'Cargando...', className = '' }: LoadingTextProps) {
  return (
    <div className={`flex items-center gap-3 py-4 ${className}`}>
      <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-700 border-t-transparent" />
      <span className="animate-pulse text-sm font-medium text-zinc-500">{message}</span>
    </div>
  )
}
