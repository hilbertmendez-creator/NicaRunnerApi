import { useState, type InputHTMLAttributes } from 'react'
import { Input } from '@nicarunner/ui'

type PasswordInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>

export function PasswordInput({ className = '', ...rest }: PasswordInputProps) {
  const [visible, setVisible] = useState(false)

  return (
    <div className="relative">
      <Input type={visible ? 'text' : 'password'} className={`pr-9 ${className}`} {...rest} />
      <button
        type="button"
        onClick={() => setVisible((prev) => !prev)}
        aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
        aria-pressed={visible}
        className="nr-password-toggle absolute inset-y-0 right-0 flex items-center px-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ac,#1d4ed8)] focus-visible:ring-offset-1 focus-visible:ring-offset-[var(--bg-card,#fff)]"
        style={{ color: 'var(--text-lo)', background: 'transparent', border: 'none', cursor: 'pointer', borderRadius: 4 }}
      >
        {visible ? (
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden="true">
            <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" />
            <circle cx="12" cy="12" r="3" />
            <line x1="3" y1="21" x2="21" y2="3" />
          </svg>
        ) : (
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden="true">
            <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" />
            <circle cx="12" cy="12" r="3" />
          </svg>
        )}
      </button>
    </div>
  )
}
