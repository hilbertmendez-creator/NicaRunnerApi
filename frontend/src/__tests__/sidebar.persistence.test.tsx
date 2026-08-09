import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AppLayout } from '../components/AppLayout'
import { renderAppShell } from '../test/renderWithProviders'

const SIDEBAR_KEY = 'nicarunner-sidebar-expanded'

vi.mock('../api/endpoints', () => ({
  getControversiasOpenCount: vi.fn().mockResolvedValue({ count: 0 }),
  getRaces: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
}))

vi.mock('../hooks/useActiveRace', () => ({
  useActiveRace: () => ({
    raceId: 7,
    races: [{ id: 7, nombre: 'Race 7', estado: 'EnCurso' }],
    activeRace: { id: 7, nombre: 'Race 7', estado: 'EnCurso' },
    loading: false,
    setRaceId: vi.fn(),
  }),
  ActiveRaceProvider: ({ children }: { children: ReactNode }) => children,
}))

describe('AppLayout sidebar expansion + mobile drawer', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('persists the expanded state to localStorage and reads it back across reload', async () => {
    // Escenario: Expansion state survives reload
    const user = userEvent.setup()
    const { container, unmount } = renderAppShell(AppLayout, { initialPath: '/' })

    const aside = container.querySelector('.nr-sidebar') as HTMLElement
    expect(aside).not.toHaveClass('expanded')
    expect(localStorage.getItem(SIDEBAR_KEY)).toBe('false')

    await user.click(screen.getByRole('button', { name: 'Expandir menú' }))

    expect(aside).toHaveClass('expanded')
    expect(screen.getByRole('button', { name: 'Contraer menú' })).toHaveAttribute('aria-expanded', 'true')
    await waitFor(() => expect(localStorage.getItem(SIDEBAR_KEY)).toBe('true'))

    unmount()

    // "Reload": a fresh mount must read the persisted value, not default to collapsed.
    const { container: container2 } = renderAppShell(AppLayout, { initialPath: '/' })
    const aside2 = container2.querySelector('.nr-sidebar') as HTMLElement
    expect(aside2).toHaveClass('expanded')
    expect(screen.getByRole('button', { name: 'Contraer menú' })).toBeInTheDocument()
  })

  it('Escape closes the mobile drawer but does not collapse the persisted desktop rail', async () => {
    // Escenario: Escape closes the mobile drawer (drawer state only, via class toggle)
    localStorage.setItem(SIDEBAR_KEY, 'true')
    const { container } = renderAppShell(AppLayout, { initialPath: '/' })

    const aside = container.querySelector('.nr-sidebar') as HTMLElement
    expect(aside).toHaveClass('expanded')
    expect(aside).toHaveClass('closed')

    // The hamburger trigger is inline display:none outside its mobile media query,
    // which also collapses its accessible name to "" under the display:none-hidden
    // exception of the accname algorithm — so it's found by class, not role/name.
    // fireEvent bypasses user-event's visibility guard to still exercise the handler.
    const mobileMenuBtn = container.querySelector('.mobile-menu-btn') as HTMLElement
    expect(mobileMenuBtn).not.toBeNull()
    fireEvent.click(mobileMenuBtn)
    expect(aside).toHaveClass('open')

    fireEvent.keyDown(window, { key: 'Escape' })

    await waitFor(() => expect(aside).toHaveClass('closed'))
    expect(aside).not.toHaveClass('open')
    // Deliberate design decision: Escape must never silently rewrite the persisted preference.
    expect(aside).toHaveClass('expanded')
    expect(screen.getByRole('button', { name: 'Contraer menú' })).toHaveAttribute('aria-expanded', 'true')
    expect(localStorage.getItem(SIDEBAR_KEY)).toBe('true')
  })
})
