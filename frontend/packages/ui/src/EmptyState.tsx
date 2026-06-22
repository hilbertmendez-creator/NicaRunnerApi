interface EmptyStateProps {
  message: string
  className?: string
}

export function EmptyState({ message, className = '' }: EmptyStateProps) {
  return (
    <div className={`flex flex-col items-center justify-center border border-dashed border-zinc-200 bg-zinc-50 p-8 text-center ${className}`}>
      <p className="text-sm font-medium text-zinc-500">{message}</p>
    </div>
  )
}
