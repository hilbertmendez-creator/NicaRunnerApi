import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { DisputeGroupDto, ResultDto } from '../api/types'
import { ResultDisputesPage } from '../features/results/ResultDisputesPage'
import { makeAuth, renderWithProviders } from '../test/renderWithProviders'

const getResultDisputes = vi.fn()
const resolveResultDispute = vi.fn()

vi.mock('../api/endpoints', () => ({
  getResultDisputes: (...args: unknown[]) => getResultDisputes(...args),
  resolveResultDispute: (...args: unknown[]) => resolveResultDispute(...args),
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

function resultado(overrides: Partial<ResultDto> = {}): ResultDto {
  return {
    id: 50,
    raceId: 9,
    runnerId: null,
    runnerNombre: 'Ana Corredora',
    dorsal: null,
    tiempoLlegada: '2026-08-09T12:00:00Z',
    posicion: 0,
    categoryId: 1,
    categoriaNombre: '5K',
    capturistaId: 3,
    capturistaNombre: 'Juez Uno',
    createdAt: '2026-08-09T12:00:00Z',
    updatedAt: '2026-08-09T12:00:00Z',
    estado: 'Controversia',
    dorsalPropuesto: '042',
    disputeGroupId: 501,
    disputeMotivo: 'DorsalDuplicado',
    elapsedMillis: null,
    ...overrides,
  }
}

function grupo(overrides: Partial<DisputeGroupDto> = {}): DisputeGroupDto {
  return {
    disputeGroupId: 501,
    raceId: 9,
    motivo: 'DorsalDuplicado',
    dorsalEnDisputa: '042',
    abiertaUtc: '2026-08-09T12:00:00Z',
    resultados: [
      resultado({ id: 50, runnerNombre: 'Ana Corredora', capturistaNombre: 'Juez Uno' }),
      resultado({ id: 51, runnerNombre: 'Beto Corredor', capturistaNombre: 'Juez Dos' }),
    ],
    ...overrides,
  }
}

describe('ResultDisputesPage API smoke', () => {
  beforeEach(() => {
    getResultDisputes.mockReset()
    resolveResultDispute.mockReset()
  })

  it('empty API shows honest empty', async () => {
    getResultDisputes.mockResolvedValue([])
    renderWithProviders(<ResultDisputesPage />, { auth: makeAuth() })

    await waitFor(() => expect(getResultDisputes).toHaveBeenCalledWith(9))
    expect(
      await screen.findByText(/No hay dorsales en disputa para esta carrera/),
    ).toBeInTheDocument()
    expect(screen.queryByText('Ana Corredora')).toBeNull()
  })

  it('renders both sides of a disputed dorsal group', async () => {
    getResultDisputes.mockResolvedValue([grupo()])
    renderWithProviders(<ResultDisputesPage />, { auth: makeAuth() })

    expect(await screen.findByText('Ana Corredora')).toBeInTheDocument()
    expect(screen.getByText('Beto Corredor')).toBeInTheDocument()
    expect(screen.getByText(/Dorsal en disputa: 042/)).toBeInTheDocument()
  })

  it('resolving assigns the winner and voids the loser with a required razon', async () => {
    const user = userEvent.setup()
    getResultDisputes.mockResolvedValue([grupo()])
    resolveResultDispute.mockResolvedValue([])
    renderWithProviders(<ResultDisputesPage />, { auth: makeAuth() })

    await screen.findByText('Ana Corredora')

    // Anula el lado de Beto — Ana se queda con el dorsal precargado (042).
    const betoRow = screen.getByText('Beto Corredor').closest('div.flex.flex-wrap')
    expect(betoRow).toBeTruthy()
    const anularButtons = screen.getAllByRole('button', { name: 'Anular' })
    await user.click(anularButtons[anularButtons.length - 1])

    await user.type(screen.getByLabelText('Razón', { exact: false }), 'Confirmado por planilla del capturista')
    await user.click(screen.getByRole('button', { name: 'Resolver disputa' }))

    await waitFor(() =>
      expect(resolveResultDispute).toHaveBeenCalledWith(9, 501, {
        asignaciones: [{ resultId: 50, dorsal: '042' }],
        anular: [51],
        razon: 'Confirmado por planilla del capturista',
      }),
    )
  })

  it('resolve failure keeps the group visible with an error', async () => {
    const user = userEvent.setup()
    getResultDisputes.mockResolvedValue([grupo()])
    resolveResultDispute.mockRejectedValue(new Error('boom'))
    renderWithProviders(<ResultDisputesPage />, { auth: makeAuth() })

    await screen.findByText('Ana Corredora')
    await user.type(screen.getByLabelText('Razón', { exact: false }), 'Intento fallido')
    await user.click(screen.getByRole('button', { name: 'Resolver disputa' }))

    expect(
      await screen.findByText(/No se pudo resolver la disputa/),
    ).toBeInTheDocument()
    expect(screen.getByText('Ana Corredora')).toBeInTheDocument()
  })
})
