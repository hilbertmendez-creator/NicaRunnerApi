import type { ButtonHTMLAttributes } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'destructive' | 'info'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: 'sm' | 'md'
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-blue-700 text-white border border-blue-700 hover:bg-blue-800',
  secondary: 'border border-zinc-200 text-zinc-700 hover:bg-zinc-50',
  destructive: 'border border-critical-200 bg-critical-50 text-critical-600 hover:border-critical-600',
  info: 'border border-official-200 bg-official-50 text-official-600 hover:border-official-600',
}

const SIZE_CLASSES = {
  sm: 'h-6 px-2 text-xs',
  md: 'h-8 px-3 text-sm',
}

export function Button({
  variant = 'secondary',
  size = 'md',
  className = '',
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={`font-medium disabled:opacity-60 ${VARIANT_CLASSES[variant]} ${SIZE_CLASSES[size]} ${className}`}
      {...rest}
    />
  )
}
