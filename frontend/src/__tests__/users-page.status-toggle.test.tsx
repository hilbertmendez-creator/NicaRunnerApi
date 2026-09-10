import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import toast from 'react-hot-toast'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { UsersPage } from '../features/users/UsersPage'
import { adminUser, makeAuth, renderWithProviders } from '../test/renderWithProviders'
import type { UserDto } from '../api/types'

const getUsers = vi.fn()
const getUserAudit = vi.fn()
const unlockUser = vi.fn()
const updateUser = vi.fn()
const getUserActiveRaces = vi.fn()

vi.mock('../api/endpoints', () => ({
  getUsers: (...args: unknown[]) => getUsers(...args),
  getUserAudit: (...args: unknown[]) => getUserAudit(...args),
  unlockUser: (...args: unknown[]) => unlockUser(...args),
  updateUser: (...args: unknown[]) => updateUser(...args),
  getUserActiveRaces: (...args: unknown[]) => getUserActiveRaces(...args),
}))

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}))

function makeUser(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: 2,
    email: 'u2@example.com',
    nombre: 'User Two',
    username: 'u2',
    role: 'Lector',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

describe('UsersPage status toggle', () => {
  beforeEach(() => {
    getUsers.mockReset()
    updateUser.mockReset()
    getUserActiveRaces.mockReset()
    // Default: sin carreras activas — preserva el flujo directo de PATCH de los tests
    // existentes salvo que un test individual pise este mock con otra resolución.
    getUserActiveRaces.mockResolvedValue([])
    vi.mocked(toast.success).mockReset()
    vi.mocked(toast.error).mockReset()
  })

  it('renders Activo for an active user and Inactivo for an inactive user', async () => {
    const active = makeUser({ id: 2, email: 'active@example.com', isActive: true })
    const inactive = makeUser({ id: 3, email: 'inactive@example.com', isActive: false })
    getUsers.mockResolvedValue({ items: [active, inactive], totalCount: 2 })

    renderWithProviders(<UsersPage />, { auth: makeAuth() })

    expect((await screen.findAllByText('Activo')).length).toBeGreaterThan(0)
    expect((await screen.findAllByText('Inactivo')).length).toBeGreaterThan(0)
  })

  it('calls updateUser with the inverted isActive value when toggled', async () => {
    const target = makeUser({ id: 2, email: 'target@example.com', isActive: true })
    getUsers.mockResolvedValue({ items: [target], totalCount: 1 })
    updateUser.mockResolvedValue({ ...target, isActive: false })

    const user = userEvent.setup()
    renderWithProviders(<UsersPage />, { auth: makeAuth() })

    const toggleButton = (await screen.findAllByRole('button', { name: 'Desactivar' }))[0]
    await user.click(toggleButton)

    await waitFor(() => {
      expect(updateUser).toHaveBeenCalledWith(2, { isActive: false })
    })
  })

  it('disables the toggle button on the signed-in admin own row', async () => {
    const self = makeUser({ id: adminUser.userId, email: adminUser.email, isActive: true })
    getUsers.mockResolvedValue({ items: [self], totalCount: 1 })

    renderWithProviders(<UsersPage />, { auth: makeAuth(adminUser) })

    const toggleButtons = await screen.findAllByRole('button', { name: 'Desactivar' })
    expect(toggleButtons.length).toBeGreaterThan(0)
    for (const button of toggleButtons) {
      expect(button).toBeDisabled()
    }
  })

  it('shows an error toast and does not reload the list when the toggle is rejected', async () => {
    const target = makeUser({ id: 2, email: 'target@example.com', isActive: true })
    getUsers.mockResolvedValue({ items: [target], totalCount: 1 })
    updateUser.mockRejectedValue(new Error('forbidden'))

    const user = userEvent.setup()
    renderWithProviders(<UsersPage />, { auth: makeAuth() })

    const toggleButton = (await screen.findAllByRole('button', { name: 'Desactivar' }))[0]
    await user.click(toggleButton)

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledTimes(1)
    })
    expect((await screen.findAllByText('Activo')).length).toBeGreaterThan(0)
    expect(getUsers).toHaveBeenCalledTimes(1)
  })

  // backoffice-user-status-toggle: "In-flight Capturista deactivation warning" (design.md D6).
  const raceManagua = { id: 10, nombre: 'Carrera Managua', fechaCarrera: '2026-01-01T00:00:00Z' }
  const raceLeon = { id: 11, nombre: 'Carrera Leon', fechaCarrera: '2026-01-02T00:00:00Z' }

  async function clickDeactivate() {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />, { auth: makeAuth() })
    const toggleButton = (await screen.findAllByRole('button', { name: 'Desactivar' }))[0]
    await user.click(toggleButton)
    return user
  }

  it('shows a confirmation dialog naming the active race when the pre-check finds one', async () => {
    getUsers.mockResolvedValue({ items: [makeUser({ id: 2, isActive: true })], totalCount: 1 })
    getUserActiveRaces.mockResolvedValue([raceManagua])

    await clickDeactivate()

    expect(await screen.findByText(/Carrera Managua/)).toBeInTheDocument()
    expect(updateUser).not.toHaveBeenCalled()
  })

  it('fires the PATCH directly with no dialog when the pre-check finds no active races', async () => {
    const target = makeUser({ id: 2, isActive: true })
    getUsers.mockResolvedValue({ items: [target], totalCount: 1 })
    getUserActiveRaces.mockResolvedValue([])
    updateUser.mockResolvedValue({ ...target, isActive: false })

    await clickDeactivate()

    await waitFor(() => expect(updateUser).toHaveBeenCalledWith(2, { isActive: false }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('names every affected race when the pre-check finds more than one', async () => {
    getUsers.mockResolvedValue({ items: [makeUser({ id: 2, isActive: true })], totalCount: 1 })
    getUserActiveRaces.mockResolvedValue([raceManagua, raceLeon])

    await clickDeactivate()

    expect(await screen.findByText(/Carrera Managua/)).toBeInTheDocument()
    expect(await screen.findByText(/Carrera Leon/)).toBeInTheDocument()
  })

  it('proceeds with the PATCH when the admin confirms the dialog', async () => {
    const target = makeUser({ id: 2, isActive: true })
    getUsers.mockResolvedValue({ items: [target], totalCount: 1 })
    getUserActiveRaces.mockResolvedValue([raceManagua])
    updateUser.mockResolvedValue({ ...target, isActive: false })

    const user = await clickDeactivate()
    await screen.findByText(/Carrera Managua/)
    await user.click(screen.getByRole('button', { name: 'Desactivar de todos modos' }))

    await waitFor(() => expect(updateUser).toHaveBeenCalledWith(2, { isActive: false }))
  })

  it('sends no PATCH and keeps the user active when the admin cancels the dialog', async () => {
    getUsers.mockResolvedValue({ items: [makeUser({ id: 2, isActive: true })], totalCount: 1 })
    getUserActiveRaces.mockResolvedValue([raceManagua])

    const user = await clickDeactivate()
    await screen.findByText(/Carrera Managua/)
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(updateUser).not.toHaveBeenCalled()
    expect(screen.queryByText(/Carrera Managua/)).not.toBeInTheDocument()
  })
})
