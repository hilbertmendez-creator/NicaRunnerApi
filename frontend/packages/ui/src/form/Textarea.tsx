import { forwardRef, type TextareaHTMLAttributes } from 'react'

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  function Textarea({ className = '', invalid = false, ...rest }, ref) {
    return (
      <textarea
        ref={ref}
        aria-invalid={invalid || undefined}
        className={`border bg-white px-3 py-2 text-sm text-zinc-900 focus:outline-none focus:ring-1 ${
          invalid
            ? 'border-critical-600 focus:border-critical-600 focus:ring-critical-600'
            : 'border-zinc-200 focus:border-blue-700 focus:ring-blue-700'
        } ${className}`}
        {...rest}
      />
    )
  },
)
