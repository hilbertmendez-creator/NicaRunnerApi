import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PublicRaceResultsDto } from '../api/types'
import { PublicResultsPage } from '../features/public-results/PublicResultsPage'
import { renderWithProviders } from '../test/renderWithProviders'

const getPublicResults = vi.fn()

vi.mock('../api/endpoints', () => ({
  getPublicResults: (...args: unknown[]) => getPublicResults(...args),
}))

function fixture(): PublicRaceResultsDto {
  return {
    raceName: 'Carrera Demo',
    fechaCarrera: '2026-08-09T00:00:00Z',
    categorias: [
      {
        nombreCategoria: '5K',
        distancia: 5,
        resultados: [
          {
            runnerId: 1,
            nombre: 'Ana Alvarez',
            dorsal: '101',
            posicion: 1,
            tiempoLlegada: '2026-08-09T12:00:00Z',
            shareKey: 'ana-key',
          },
          {
            runnerId: 2,
            nombre: 'Beto Blanco',
            dorsal: '102',
            posicion: 2,
            tiempoLlegada: '2026-08-09T12:05:00Z',
            shareKey: 'beto-key',
          },
        ],
      },
      {
        nombreCategoria: '10K',
        distancia: 10,
        resultados: [
          {
            runnerId: 3,
            nombre: 'Carla Cruz',
            dorsal: '201',
            posicion: 1,
            tiempoLlegada: '2026-08-09T12:10:00Z',
            shareKey: 'carla-key',
          },
        ],
      },
    ],
  }
}

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/resultados/:token" element={<PublicResultsPage />} />
    </Routes>,
    { initialPath: '/resultados/abc' },
  )
}

describe('PublicResultsPage per-category filter', () => {
  beforeEach(() => {
    getPublicResults.mockReset()
    getPublicResults.mockResolvedValue(fixture())
  })

  it('filtering one category leaves the other category table/count untouched', async () => {
    // Escenario: Filtering one category leaves others unchanged
    const user = userEvent.setup()
    renderPage()

    await waitFor(() => expect(getPublicResults).toHaveBeenCalledWith('abc'))
    expect(await screen.findByText('Ana Alvarez')).toBeInTheDocument()

    const section5k = screen.getByText('5K (5 km)').closest('section') as HTMLElement
    const section10k = screen.getByText('10K (10 km)').closest('section') as HTMLElement

    await user.type(within(section5k).getByLabelText('Filtrar corredores'), 'Ana')

    expect(within(section5k).getByText('Ana Alvarez')).toBeInTheDocument()
    expect(within(section5k).queryByText('Beto Blanco')).toBeNull()
    expect(within(section5k).getByText('1 de 2 corredores')).toBeInTheDocument()

    // Category B never re-renders/filters as a side effect of typing in A's input.
    expect(within(section10k).getByText('Carla Cruz')).toBeInTheDocument()
    expect(within(section10k).getByText('1 de 1 corredores')).toBeInTheDocument()
    expect(within(section10k).getByLabelText('Filtrar corredores')).toHaveValue('')
  })

  it('empty-after-filter shows a category-scoped EmptyState with a working "Limpiar filtro" action', async () => {
    // Escenario: Empty filter result shows category-scoped empty state
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Ana Alvarez')
    const section5k = screen.getByText('5K (5 km)').closest('section') as HTMLElement

    await user.type(within(section5k).getByLabelText('Filtrar corredores'), 'nadie-coincide')

    expect(within(section5k).getByText('Ningún corredor coincide con el filtro.')).toBeInTheDocument()
    expect(within(section5k).queryByText('Ana Alvarez')).toBeNull()

    await user.click(within(section5k).getByRole('button', { name: 'Limpiar filtro' }))

    expect(within(section5k).getByText('Ana Alvarez')).toBeInTheDocument()
    expect(within(section5k).getByText('2 de 2 corredores')).toBeInTheDocument()
  })
})
