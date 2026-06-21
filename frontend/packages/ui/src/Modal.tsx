import { useEffect, useRef, type ReactNode } from 'react'

interface ModalProps {
  onClose: () => void
  children: ReactNode
  maxWidth?: 'md' | 'lg'
  labelledBy?: string
}

const MAX_WIDTH_CLASSES = {
  md: 'max-w-md',
  lg: 'max-w-lg',
}

export function Modal({ onClose, children, maxWidth = 'md', labelledBy }: ModalProps) {
  const cardRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null
    const focusable = cardRef.current?.querySelector<HTMLElement>(
      'input, textarea, select, button',
    )
    focusable?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocused?.focus()
    }
  }, [onClose])

  return (
    <div
      className="fixed inset-0 flex items-center justify-center bg-black/30"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose()
      }}
    >
      <div
        ref={cardRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledBy}
        className={`w-full ${MAX_WIDTH_CLASSES[maxWidth]} rounded-lg bg-white p-6 shadow-lg`}
      >
        {children}
      </div>
    </div>
  )
}
