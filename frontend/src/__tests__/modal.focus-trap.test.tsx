import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Modal } from '@nicarunner/ui'

function renderModal() {
  return render(
    <Modal onClose={vi.fn()} labelledBy="title">
      <h2 id="title">Título</h2>
      <button type="button">Primero</button>
      <button type="button">Segundo</button>
      <button type="button">Último</button>
    </Modal>,
  )
}

describe('Modal focus trap', () => {
  it('Tab from the last control wraps to the first instead of escaping the modal', async () => {
    const user = userEvent.setup()
    renderModal()

    const last = screen.getByRole('button', { name: 'Último' })
    last.focus()
    expect(document.activeElement).toBe(last)

    await user.tab()

    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Primero' }))
  })

  it('Shift+Tab from the first control wraps to the last instead of escaping the modal', async () => {
    const user = userEvent.setup()
    renderModal()

    const first = screen.getByRole('button', { name: 'Primero' })
    first.focus()
    expect(document.activeElement).toBe(first)

    await user.tab({ shift: true })

    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Último' }))
  })
})
