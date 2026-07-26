import { Button } from './Button'

interface EmptyStateProps {
  message: string
  className?: string
  action?: {
    label: string
    onClick: () => void
  }
}

export function EmptyState({ message, className = '', action }: EmptyStateProps) {
  return (
    <div className={`flex flex-col items-center justify-center border border-dashed border-zinc-200 bg-zinc-50 p-8 text-center ${className}`}>
      <p className="text-sm font-medium text-zinc-500">{message}</p>
      {action && (
        <Button variant="secondary" size="sm" className="mt-3" onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </div>
  )
}
