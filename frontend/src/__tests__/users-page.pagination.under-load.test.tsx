import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { UsersPage } from '../features/users/UsersPage'
import { makeAuth, renderWithProviders } from '../test/renderWithProviders'
import type { UserDto } from '../api/types'

const getUsers = vi.fn()
const getUserAudit = vi.fn()
const unlockUser = vi.fn()
const updateUser = vi.fn()

vi.mock('../api/endpoints', () => ({
  getUsers: (...args: unknown[]) => getUsers(...args),
  getUserAudit: (...args: unknown[]) => getUserAudit(...args),
  unlockUser: (...args: unknown[]) => unlockUser(...args),
  updateUser: (...args: unknown[]) => updateUser(...args),
}))

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}))

function makeUsers(count: number): UserDto[] {
  return Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    email: `u${i + 1}@example.com`,
    nombre: `User ${i + 1}`,
    username: `u${i + 1}`,
    role: 'Lector' as const,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
  }))
}

describe('UsersPage pagination under load', () => {
  const all = makeUsers(120) // > 1 page at pageSize 50
  const pageSize = 50

  beforeEach(() => {
    getUsers.mockReset()
    getUsers.mockImplementation(async (limit: number, offset: number) => ({
      items: all.slice(offset, offset + limit),
      totalCount: all.length,
    }))
  })

  it('requests offset 0 on page 1 then offset 50 after Next', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />, { auth: makeAuth() })

    await waitFor(() => {
      expect(getUsers).toHaveBeenCalledWith(pageSize, 0)
    })
    expect((await screen.findAllByText('u1@example.com')).length).toBeGreaterThan(0)

    const next = (await screen.findAllByRole('button', { name: /Siguiente/i }))[0]
    await user.click(next)

    await waitFor(() => {
      expect(getUsers).toHaveBeenCalledWith(pageSize, 50)
    })
    expect((await screen.findAllByText('u51@example.com')).length).toBeGreaterThan(0)
    expect(screen.queryAllByText('u1@example.com')).toHaveLength(0)
  })
})
