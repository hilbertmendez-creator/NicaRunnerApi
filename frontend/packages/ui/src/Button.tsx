import type { ButtonHTMLAttributes, CSSProperties } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'destructive' | 'info'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: 'sm' | 'md'
}

const VARIANT_STYLES: Record<ButtonVariant, CSSProperties> = {
  primary: {
    background: 'var(--ac, #2563EB)',
    color: '#fff',
    border: '1px solid var(--ac, #2563EB)',
    borderRadius: 'var(--radius-btn, 6px)',
  },
  secondary: {
    background: 'var(--bg-card, #fff)',
    color: 'var(--text-hi, #3f3f46)',
    border: '1px solid var(--bd, #e4e4e7)',
    borderRadius: 'var(--radius-btn, 6px)',
  },
  destructive: {
    background: 'var(--er-bg, #fef2f2)',
    color: 'var(--er-tx, #dc2626)',
    border: '1px solid var(--er-bd, #fecaca)',
    borderRadius: 'var(--radius-btn, 6px)',
  },
  info: {
    background: 'var(--ok-bg, #f0fdf4)',
    color: 'var(--ok-tx, #15803d)',
    border: '1px solid var(--ok-bd, #bbf7d0)',
    borderRadius: 'var(--radius-btn, 6px)',
  },
}

const SIZE_CLASSES = {
  sm: 'nr-ui-btn-sm h-6 px-2 text-xs',
  md: 'nr-ui-btn-md h-8 px-3 text-sm',
}

export function Button({
  variant = 'secondary',
  size = 'md',
  className = '',
  type = 'button',
  style,
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={`font-medium disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ac,#1d4ed8)] focus-visible:ring-offset-1 focus-visible:ring-offset-[var(--bg-app,#fff)] ${SIZE_CLASSES[size]} ${className}`}
      style={{ ...VARIANT_STYLES[variant], ...style }}
      {...rest}
    />
  )
}
