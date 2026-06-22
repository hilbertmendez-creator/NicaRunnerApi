import { forwardRef, type TextareaHTMLAttributes } from 'react'

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  function Textarea({ className = '', ...rest }, ref) {
    return (
      <textarea
        ref={ref}
        className={`border border-zinc-200 bg-white px-3 py-2 text-sm text-zinc-900 focus:border-blue-700 focus:outline-none focus:ring-1 focus:ring-blue-700 ${className}`}
        {...rest}
      />
    )
  },
)
