interface ErrorAlertProps {
  message: string
  className?: string
  onRetry?: () => void
  retryLabel?: string
}

export function ErrorAlert({
  message,
  className = '',
  onRetry,
  retryLabel = 'Reintentar',
}: ErrorAlertProps) {
  return (
    <div
      role="alert"
      className={`border p-4 ${className}`}
      style={{
        borderColor: 'var(--er-bd, #fecaca)',
        background: 'var(--er-bg, #fef2f2)',
        borderRadius: 'var(--radius-card, 7px)',
      }}
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <p
          className="min-w-0 text-sm font-medium"
          style={{ color: 'var(--er-tx, #b91c1c)', overflowWrap: 'anywhere' }}
        >
          {message}
        </p>
        {onRetry && (
          <button
            type="button"
            onClick={onRetry}
            className="nr-ui-btn-sm shrink-0 px-3 py-1.5 text-sm font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ac,#1d4ed8)] focus-visible:ring-offset-1 focus-visible:ring-offset-[var(--bg-card,#fff)]"
            style={{
              background: 'var(--bg-card, #fff)',
              color: 'var(--text-hi, #18181b)',
              border: '1px solid var(--bd, #e4e4e7)',
              borderRadius: 'var(--radius-btn, 6px)',
              cursor: 'pointer',
            }}
          >
            {retryLabel}
          </button>
        )}
      </div>
    </div>
  )
}
