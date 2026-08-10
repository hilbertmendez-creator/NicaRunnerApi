import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AppLayout } from '../components/AppLayout'
import { makeAuth, renderAppShell } from '../test/renderWithProviders'

const getOpenCount = vi.fn()
const getAdminNotifications = vi.fn()

vi.mock('../api/endpoints', () => ({
  getControversiasOpenCount: (...args: unknown[]) => getOpenCount(...args),
  getRaces: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
  getAdminNotifications: (...args: unknown[]) => getAdminNotifications(...args),
  markAdminNotificationRead: vi.fn(),
  markAllAdminNotificationsRead: vi.fn(),
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

describe('AppLayout honesty smoke', () => {
  beforeEach(() => {
    getOpenCount.mockReset()
    getOpenCount.mockResolvedValue({ count: 0 })
    getAdminNotifications.mockReset()
    getAdminNotifications.mockResolvedValue({ items: [], unreadCount: 0 })
  })

  it('Controversias has own route and active state; Resultados stays inactive', async () => {
    // Escenarios verify: Controversias route/active + Notificaciones/Enlaces discoverable
    const user = userEvent.setup()
    renderAppShell(AppLayout, { initialPath: '/' })

    const nav = screen.getByRole('navigation', { name: 'Navegación principal' })
    expect(within(nav).getByRole('button', { name: 'Notificaciones' })).toBeInTheDocument()
    expect(within(nav).getByRole('button', { name: 'Enlaces' })).toBeInTheDocument()

    await user.click(within(nav).getByRole('button', { name: 'Controversias' }))
    expect(await screen.findByText('Controversias outlet')).toBeInTheDocument()

    expect(within(nav).getByRole('button', { name: 'Controversias' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(within(nav).getByRole('button', { name: 'Resultados' })).not.toHaveAttribute(
      'aria-current',
    )
  })

  it('bell opens the admin notifications dropdown instead of navigating to the runner-notify page', async () => {
    // Escenario: la campana ya no redirige a /notificaciones (esa pantalla es "notificar
    // a corredores", una feature distinta) — ahora abre un feed propio del admin in-place.
    const user = userEvent.setup()
    renderAppShell(AppLayout, { initialPath: '/' })

    const header = screen.getByRole('banner')
    const bell = within(header).getByRole('button', { name: 'Notificaciones' })
    // Sin no-leídas: sin punto/badge de urgencia falsa en el control del topbar.
    expect(bell.querySelector('[aria-hidden="true"][style*="EF4444"]')).toBeNull()

    await user.click(bell)

    expect(screen.getByRole('menu', { name: 'Notificaciones' })).toBeInTheDocument()
    expect(screen.queryByText('Notificaciones outlet')).toBeNull()
  })

  it('badge honesty: hidden when open-count is 0, shown when > 0', async () => {
    // Escenario: Badge honesty (OQ-4)
    getOpenCount.mockResolvedValue({ count: 0 })
    const { unmount } = renderAppShell(AppLayout, { auth: makeAuth(), initialPath: '/controversias' })
    await waitFor(() => expect(getOpenCount).toHaveBeenCalledWith(7))
    expect(screen.getByRole('button', { name: 'Controversias' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Controversias \(\d+ abiertas\)/ })).toBeNull()
    unmount()

    getOpenCount.mockResolvedValue({ count: 3 })
    renderAppShell(AppLayout, { initialPath: '/controversias' })
    expect(
      await screen.findByRole('button', { name: 'Controversias (3 abiertas)' }),
    ).toBeInTheDocument()
  })
})
