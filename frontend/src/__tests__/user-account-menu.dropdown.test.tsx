import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeAll, describe, expect, it } from 'vitest'
import { UserAccountMenu } from '../components/UserAccountMenu'
import { makeAuth, renderWithProviders } from '../test/renderWithProviders'

// Réplica exacta de la regla CSS de producción (index.css) que hace el dropdown
// inerte al teclado mientras está cerrado. vitest.config's `test.css: false`
// no carga index.css en jsdom, así que se inyecta aquí para ejercer el
// mecanismo visibility:hidden + pointer-events:none real, no solo el estado de React.
beforeAll(() => {
  const style = document.createElement('style')
  style.textContent = `
    .user-menu-dropdown { visibility: hidden; pointer-events: none; }
    .user-menu.open .user-menu-dropdown { visibility: visible; pointer-events: auto; }
  `
  document.head.appendChild(style)
})

describe('UserAccountMenu dropdown', () => {
  it('toggles the "open" class without unmounting the dropdown node', async () => {
    const user = userEvent.setup()
    const { container } = renderWithProviders(<UserAccountMenu />, { auth: makeAuth() })

    const dropdown = container.querySelector('.user-menu-dropdown')
    expect(dropdown).not.toBeNull()
    expect(container.querySelector('.user-menu')).not.toHaveClass('open')

    await user.click(screen.getByRole('button', { name: 'Menú de cuenta' }))

    // Mismo nodo — no un remount condicional.
    expect(container.querySelector('.user-menu-dropdown')).toBe(dropdown)
    expect(container.querySelector('.user-menu')).toHaveClass('open')
  })

  it('closed state is keyboard-inert: Tab does not reach "Cerrar sesión"', async () => {
    // Escenario: Tab skips hidden logout control
    const user = userEvent.setup()
    const { container } = renderWithProviders(<UserAccountMenu />, { auth: makeAuth() })

    const logoutBtn = container.querySelector('.nr-account-logout') as HTMLElement
    expect(logoutBtn).not.toBeNull()

    screen.getByRole('button', { name: 'Menú de cuenta' }).focus()
    await user.tab()

    expect(document.activeElement).not.toBe(logoutBtn)
    // aria-hidden on the closed dropdown also excludes it from the a11y tree.
    expect(screen.queryByRole('menuitem', { name: 'Cerrar sesión' })).toBeNull()
  })

  it('opening reaches "Cerrar sesión"; clicking outside closes the menu', async () => {
    // Escenario: Menu opens and closes with transition
    const user = userEvent.setup()
    const { container } = renderWithProviders(<UserAccountMenu />, { auth: makeAuth() })

    await user.click(screen.getByRole('button', { name: 'Menú de cuenta' }))
    expect(container.querySelector('.user-menu')).toHaveClass('open')
    expect(screen.getByRole('menuitem', { name: 'Cerrar sesión' })).toBeInTheDocument()

    await user.click(document.body)

    expect(container.querySelector('.user-menu')).not.toHaveClass('open')
    expect(screen.queryByRole('menuitem', { name: 'Cerrar sesión' })).toBeNull()
  })
})
