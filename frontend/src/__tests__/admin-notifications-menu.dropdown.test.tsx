import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminNotificationsMenu } from '../components/AdminNotificationsMenu'
import { renderWithProviders } from '../test/renderWithProviders'

const getAdminNotifications = vi.fn()
const markAdminNotificationRead = vi.fn()
const markAllAdminNotificationsRead = vi.fn()

vi.mock('../api/endpoints', () => ({
  getAdminNotifications: (...args: unknown[]) => getAdminNotifications(...args),
  markAdminNotificationRead: (...args: unknown[]) => markAdminNotificationRead(...args),
  markAllAdminNotificationsRead: (...args: unknown[]) => markAllAdminNotificationsRead(...args),
}))

// Réplica de la regla CSS de producción — ver user-account-menu.dropdown.test.tsx,
// el componente reusa las mismas clases .user-menu/.user-menu-dropdown.
beforeAll(() => {
  const style = document.createElement('style')
  style.textContent = `
    .user-menu-dropdown { visibility: hidden; pointer-events: none; }
    .user-menu.open .user-menu-dropdown { visibility: visible; pointer-events: auto; }
  `
  document.head.appendChild(style)
})

const UNREAD_ITEM = {
  id: 1,
  type: 'DorsalDuplicado' as const,
  mensaje: "Dorsal '105' duplicado en la carrera #1.",
  raceId: 1,
  createdAt: '2026-01-01T08:00:00Z',
  leida: false,
}

const READ_ITEM = {
  id: 2,
  type: 'UsuarioCreado' as const,
  mensaje: 'Se creó el usuario Ana (Capturista).',
  raceId: null,
  createdAt: '2026-01-01T07:00:00Z',
  leida: true,
}

describe('AdminNotificationsMenu', () => {
  beforeEach(() => {
    getAdminNotifications.mockReset()
    markAdminNotificationRead.mockReset()
    markAllAdminNotificationsRead.mockReset()
  })

  it('shows an unread badge and lists items when opened; clicking an unread item marks it read', async () => {
    getAdminNotifications.mockResolvedValue({ items: [UNREAD_ITEM, READ_ITEM], unreadCount: 1 })
    markAdminNotificationRead.mockResolvedValue(undefined)
    const user = userEvent.setup()

    const { container } = renderWithProviders(<AdminNotificationsMenu />)

    await waitFor(() => expect(getAdminNotifications).toHaveBeenCalled())
    expect(screen.getByText('1')).toBeInTheDocument() // badge

    const bell = screen.getByRole('button', { name: 'Notificaciones (1 sin leer)' })
    await user.click(bell)

    expect(container.querySelector('.user-menu')).toHaveClass('open')
    expect(screen.getByText("Dorsal '105' duplicado en la carrera #1.")).toBeInTheDocument()
    expect(screen.getByText('Se creó el usuario Ana (Capturista).')).toBeInTheDocument()

    await user.click(screen.getByText("Dorsal '105' duplicado en la carrera #1."))

    expect(markAdminNotificationRead).toHaveBeenCalledWith(1)
    // Ya leída: un segundo click no vuelve a marcar.
    markAdminNotificationRead.mockClear()
    await user.click(screen.getByText('Se creó el usuario Ana (Capturista).'))
    expect(markAdminNotificationRead).not.toHaveBeenCalled()
  })

  it('marks all as read and shows an honest empty state with zero unread', async () => {
    getAdminNotifications.mockResolvedValue({ items: [], unreadCount: 0 })
    const user = userEvent.setup()

    renderWithProviders(<AdminNotificationsMenu />)

    await waitFor(() => expect(getAdminNotifications).toHaveBeenCalled())

    // Sin no-leídas: ni badge numérico ni botón "Marcar todas como leídas".
    expect(screen.getByRole('button', { name: 'Notificaciones' })).toBeInTheDocument()
    expect(screen.queryByText('Marcar todas como leídas')).toBeNull()

    await user.click(screen.getByRole('button', { name: 'Notificaciones' }))

    expect(screen.getByText('Sin notificaciones.')).toBeInTheDocument()
  })

  it('"Marcar todas como leídas" calls the bulk endpoint and refetches', async () => {
    getAdminNotifications
      .mockResolvedValueOnce({ items: [UNREAD_ITEM], unreadCount: 1 })
      .mockResolvedValue({ items: [{ ...UNREAD_ITEM, leida: true }], unreadCount: 0 })
    markAllAdminNotificationsRead.mockResolvedValue(undefined)
    const user = userEvent.setup()

    renderWithProviders(<AdminNotificationsMenu />)

    await user.click(await screen.findByRole('button', { name: 'Notificaciones (1 sin leer)' }))
    await user.click(screen.getByText('Marcar todas como leídas'))

    expect(markAllAdminNotificationsRead).toHaveBeenCalledOnce()
    await waitFor(() => expect(getAdminNotifications).toHaveBeenCalledTimes(2))
  })

  it('labels the race lifecycle events instead of showing a raw enum name', async () => {
    getAdminNotifications.mockResolvedValue({
      items: [
        {
          id: 10,
          type: 'CarreraCerradaPorJuez' as const,
          mensaje: 'El juez Ana cerró la carrera "5K Managua".',
          raceId: 7,
          createdAt: '2026-01-01T09:00:00Z',
          leida: false,
        },
        {
          id: 11,
          type: 'CarrerasSinActividad' as const,
          mensaje: '2 carreras siguen EnCurso sin capturas recientes: "5K Managua", "10K León".',
          raceId: null,
          createdAt: '2026-01-01T08:30:00Z',
          leida: false,
        },
      ],
      unreadCount: 2,
    })
    const user = userEvent.setup()

    renderWithProviders(<AdminNotificationsMenu />)

    await user.click(await screen.findByRole('button', { name: 'Notificaciones (2 sin leer)' }))

    expect(screen.getByText('Carrera cerrada por un juez')).toBeInTheDocument()
    expect(screen.getByText('Carreras sin actividad')).toBeInTheDocument()
    expect(screen.getByText('El juez Ana cerró la carrera "5K Managua".')).toBeInTheDocument()
  })
})
