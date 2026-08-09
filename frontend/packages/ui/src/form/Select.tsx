import { forwardRef, type CSSProperties, type SelectHTMLAttributes } from 'react'

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  function Select({ className = '', invalid = false, style, ...rest }, ref) {
    const fieldStyle: CSSProperties = {
      backgroundColor: 'var(--bg-input, #fff)',
      color: 'var(--text-hi, #18181b)',
      border: `1px solid ${invalid ? 'var(--er-tx, #dc2626)' : 'var(--bd, #e4e4e7)'}`,
      borderRadius: 'var(--radius-btn, 6px)',
      ...style,
    }

    return (
      <select
        ref={ref}
        aria-invalid={invalid || undefined}
        className={`nr-ui-input nr-ui-select h-8 pl-3 pr-7 text-sm focus:outline-none focus:ring-1 ${
          invalid
            ? 'focus:border-[var(--er-tx,#dc2626)] focus:ring-[var(--er-tx,#dc2626)]'
            : 'focus:border-[var(--ac,#1d4ed8)] focus:ring-[var(--ac,#1d4ed8)]'
        } ${className}`}
        style={fieldStyle}
        {...rest}
      />
    )
  },
)
