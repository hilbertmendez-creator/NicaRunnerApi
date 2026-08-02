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
    <div
      className={`flex flex-col items-center justify-center border border-dashed p-8 text-center ${className}`}
      style={{
        borderColor: 'var(--bd, #e4e4e7)',
        background: 'var(--bg-input, #fafafa)',
        borderRadius: 'var(--radius-card, 7px)',
      }}
    >
      <p className="text-sm font-medium" style={{ color: 'var(--text-lo, #71717a)' }}>
        {message}
      </p>
      {action && (
        <Button variant="secondary" size="sm" className="mt-3" onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </div>
  )
}
