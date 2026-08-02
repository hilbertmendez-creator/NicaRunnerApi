import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { TimingDisputeDto } from '../api/types'
import { ControversiasPage } from '../features/controversias/ControversiasPage'
import { makeAuth, renderWithProviders } from '../test/renderWithProviders'

const getControversias = vi.fn()
const resolveControversia = vi.fn()

vi.mock('../api/endpoints', () => ({
  getControversias: (...args: unknown[]) => getControversias(...args),
  resolveControversia: (...args: unknown[]) => resolveControversia(...args),
}))

vi.mock('../hooks/useActiveRace', () => ({
  useActiveRace: () => ({
    raceId: 9,
    races: [{ id: 9, nombre: 'Race 9', estado: 'EnCurso' }],
    activeRace: { id: 9, nombre: 'Race 9', estado: 'EnCurso' },
    loading: false,
    setRaceId: vi.fn(),
  }),
  ActiveRaceProvider: ({ children }: { children: ReactNode }) => children,
}))

function dispute(overrides: Partial<TimingDisputeDto> = {}): TimingDisputeDto {
  return {
    id: 101,
    raceId: 9,
    resultId: 50,
    runnerId: 3,
    dorsal: '042',
    corredorNombre: 'Ana Corredora',
    chipSeconds: 3600,
    checkpointSeconds: 3605,
    cameraSeconds: null,
    estado: 'Revision',
    capturistaNombre: 'Juez Uno',
    capturaNote: null,
    resolvedByUserId: null,
    resolvedAt: null,
    updatedAt: '2026-08-02T12:00:00Z',
    ...overrides,
  }
}

describe('ControversiasPage API smoke', () => {
  beforeEach(() => {
    getControversias.mockReset()
    resolveControversia.mockReset()
  })

  it('empty API shows honest empty — never mock live ops rows', async () => {
    // Escenarios: no mock as live ops; honest empty; delivery gated on API empty
    getControversias.mockResolvedValue([])
    renderWithProviders(<ControversiasPage />, { auth: makeAuth() })

    await waitFor(() => expect(getControversias).toHaveBeenCalledWith(9))
    expect(
      await screen.findByText(/No hay controversias abiertas para esta carrera/),
    ).toBeInTheDocument()
    expect(screen.queryByText('Ana Corredora')).toBeNull()
    expect(document.body.textContent).not.toMatch(/MOCK_ROWS/)
  })

  it('grid loads from mocked getControversias', async () => {
    // Escenario: Grid loads from API
    getControversias.mockResolvedValue([dispute()])
    renderWithProviders(<ControversiasPage />, { auth: makeAuth() })

    expect(await screen.findByText('Ana Corredora')).toBeInTheDocument()
    expect(screen.getByText('042')).toBeInTheDocument()
    expect(screen.getAllByText('Revisión').length).toBeGreaterThanOrEqual(1)
  })

  it('Oficial confirm UX calls resolveControversia with source + confirm', async () => {
    // Escenario: Oficial/Disputa call API
    const user = userEvent.setup()
    getControversias.mockResolvedValue([dispute()])
    resolveControversia.mockResolvedValue(dispute({ estado: 'Oficial' }))
    renderWithProviders(<ControversiasPage />, { auth: makeAuth() })

    const row = (await screen.findByText('Ana Corredora')).closest('tr')
    expect(row).toBeTruthy()
    await user.click(within(row as HTMLElement).getByRole('button', { name: 'Marcar oficial' }))

    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByRole('heading', { name: /Confirmar tiempo oficial/ })).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar tiempo oficial' }))

    await waitFor(() =>
      expect(resolveControversia).toHaveBeenCalledWith(9, 101, {
        estado: 'Oficial',
        selectedSource: 'Chip',
        confirmApplyOfficial: true,
      }),
    )
  })

  it('Disputa confirm path calls resolveControversia', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    getControversias.mockResolvedValue([dispute()])
    resolveControversia.mockResolvedValue(dispute({ estado: 'Disputa' }))
    renderWithProviders(<ControversiasPage />, { auth: makeAuth() })

    const row = (await screen.findByText('Ana Corredora')).closest('tr')
    expect(row).toBeTruthy()
    await user.click(within(row as HTMLElement).getByRole('button', { name: 'Marcar disputa' }))

    await waitFor(() =>
      expect(resolveControversia).toHaveBeenCalledWith(9, 101, { estado: 'Disputa' }),
    )
    confirmSpy.mockRestore()
  })
})
