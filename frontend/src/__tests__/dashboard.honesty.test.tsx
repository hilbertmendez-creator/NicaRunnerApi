import { screen, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { NotificationsPage } from '../features/notifications/NotificationsPage'
import { PublicLinksPage } from '../features/public-links/PublicLinksPage'
import { ResultsPage } from '../features/results/ResultsPage'
import { makeAuth, renderWithProviders } from '../test/renderWithProviders'

const getDashboard = vi.fn()
const getStandings = vi.fn()
const getResults = vi.fn()
const getPublicTokens = vi.fn()

vi.mock('../api/endpoints', () => ({
  getDashboard: (...args: unknown[]) => getDashboard(...args),
  getStandings: (...args: unknown[]) => getStandings(...args),
  getResults: (...args: unknown[]) => getResults(...args),
  getPublicTokens: (...args: unknown[]) => getPublicTokens(...args),
  notifyAll: vi.fn(),
  notifyResult: vi.fn(),
  createPublicToken: vi.fn(),
}))

vi.mock('../hooks/useActiveRace', () => ({
  useActiveRace: () => ({
    raceId: 11,
    races: [{ id: 11, nombre: 'Demo Race', estado: 'EnCurso' }],
    activeRace: { id: 11, nombre: 'Demo Race', estado: 'EnCurso' },
    loading: false,
    setRaceId: vi.fn(),
  }),
  ActiveRaceProvider: ({ children }: { children: ReactNode }) => children,
}))

vi.mock('../hooks/useRaceDashboardHub', () => ({
  useRaceDashboardHub: () => {},
}))

describe('Dashboard + peer chrome honesty smoke', () => {
  beforeEach(() => {
    getDashboard.mockReset()
    getStandings.mockReset()
    getResults.mockReset()
    getPublicTokens.mockReset()
    getStandings.mockResolvedValue([])
    getResults.mockResolvedValue([])
    getPublicTokens.mockResolvedValue([])
    getDashboard.mockResolvedValue({
      raceId: 11,
      raceName: 'Demo Race',
      estado: 'EnCurso',
      totalInscritos: 42,
      totalConTiempo: 17,
      totalPendientes: 5,
      categorias: [],
      ultimosResultados: [],
      tiempoEnCursoSegundos: null,
      ritmoPromedioSegundosPorKm: null,
    })
  })

  it('KPIs without backing data show an honest — with a reason, not Próximamente; live counts bind API', async () => {
    // Escenarios: honest empty per-KPI reason, live bind, no duplicate race chrome
    renderWithProviders(<DashboardPage />, { auth: makeAuth() })

    await waitFor(() => expect(getDashboard).toHaveBeenCalledWith(11))

    expect(screen.getByText('Tiempo en curso')).toBeInTheDocument()
    expect(screen.getByText('Ritmo promedio')).toBeInTheDocument()
    expect(screen.getByText('Último dorsal')).toBeInTheDocument()
    expect(screen.queryByText('Cámara ok')).toBeNull()
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(3)
    expect(screen.getByText('Carrera no iniciada')).toBeInTheDocument()
    expect(screen.getByText('Sin tiempos registrados')).toBeInTheDocument()
    expect(screen.getByText('Sin capturas todavía')).toBeInTheDocument()

    expect(screen.getByText('Chip llegadas')).toBeInTheDocument()
    expect(screen.getAllByText('17').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Pendientes').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('5').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText(/▲ de 42 inscritos/)).toBeInTheDocument()

    // Sin RaceSelector de página (chrome solo en topbar cuando hay AppLayout).
    expect(screen.queryByLabelText('Carrera activa')).toBeNull()
    expect(document.body.textContent).not.toMatch(/MOCK_ROWS|CONNECTION_CYCLE/)
  })

  it('KPIs render live values once tiempo en curso, ritmo promedio and último dorsal have data', async () => {
    // Escenario: la carrera arrancó y tiene capturas — los 3 KPIs antes aspiracionales muestran datos reales.
    getDashboard.mockResolvedValue({
      raceId: 11,
      raceName: 'Demo Race',
      estado: 'EnCurso',
      totalInscritos: 42,
      totalConTiempo: 17,
      totalPendientes: 5,
      categorias: [],
      ultimosResultados: [
        {
          resultId: 1,
          dorsal: '101',
          nombre: 'Ana Pérez',
          tiempoLlegada: '2026-01-01T08:30:00Z',
          posicion: 1,
          nombreCategoria: 'Libre',
          capturistaId: 1,
        },
      ],
      tiempoEnCursoSegundos: 1830,
      ritmoPromedioSegundosPorKm: 330,
    })

    renderWithProviders(<DashboardPage />, { auth: makeAuth() })

    await waitFor(() => expect(getDashboard).toHaveBeenCalledWith(11))

    expect(screen.getByText('30:30')).toBeInTheDocument()
    expect(screen.getByText('5:30/km')).toBeInTheDocument()
    // "101" aparece tanto en el KPI "Último dorsal" como en la fila de la tabla de capturas.
    expect(screen.getAllByText('101').length).toBeGreaterThanOrEqual(2)
  })

  it('peer screens keep race chrome out of page body', async () => {
    // Escenario PARTIAL: peer-screen chrome audit (runtime assertion)
    renderWithProviders(<ResultsPage />, { auth: makeAuth() })
    await waitFor(() => expect(getResults).toHaveBeenCalled())
    expect(screen.queryByLabelText('Carrera activa')).toBeNull()

    const { unmount: u1 } = renderWithProviders(<NotificationsPage />, { auth: makeAuth() })
    expect(screen.getByText('Notificaciones')).toBeInTheDocument()
    expect(screen.queryByLabelText('Carrera activa')).toBeNull()
    u1()

    renderWithProviders(<PublicLinksPage />, { auth: makeAuth() })
    await waitFor(() => expect(getPublicTokens).toHaveBeenCalled())
    expect(screen.queryByLabelText('Carrera activa')).toBeNull()
  })
})
