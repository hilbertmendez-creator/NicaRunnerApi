interface ErrorAlertProps {
  message: string
  className?: string
}

export function ErrorAlert({ message, className = '' }: ErrorAlertProps) {
  return (
    <div role="alert" className={`border border-critical-200 bg-critical-50 p-4 ${className}`}>
      <div className="flex gap-2">
        <span className="text-sm font-semibold text-critical-700">Error:</span>
        <p className="text-sm font-medium text-critical-700">{message}</p>
      </div>
    </div>
  )
}
