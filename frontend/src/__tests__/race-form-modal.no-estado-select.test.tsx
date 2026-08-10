import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RaceDto } from '../api/types'
import { RaceFormModal } from '../features/races/RaceFormModal'
import { renderWithProviders } from '../test/renderWithProviders'

const updateRace = vi.fn()
const createRace = vi.fn()
const getCategoryCatalog = vi.fn()

vi.mock('../api/endpoints', () => ({
  updateRace: (...args: unknown[]) => updateRace(...args),
  createRace: (...args: unknown[]) => createRace(...args),
  getCategoryCatalog: (...args: unknown[]) => getCategoryCatalog(...args),
}))

function race(overrides: Partial<RaceDto> = {}): RaceDto {
  return {
    id: 7,
    nombre: '5K Managua',
    descripcion: 'Carrera de prueba',
    fechaCarrera: '2026-09-01T00:00:00Z',
    estado: 'EnCurso',
    joinCode: 'AB12CD',
    adminId: 1,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

describe('RaceFormModal — Correction B: Race.Estado es derivado, ya no se edita acá', () => {
  beforeEach(() => {
    updateRace.mockReset()
    createRace.mockReset()
    getCategoryCatalog.mockReset()
  })

  it('editando una carrera existente no muestra ningún control de Estado', () => {
    renderWithProviders(<RaceFormModal race={race()} onClose={vi.fn()} onSaved={vi.fn()} />)

    expect(screen.getByLabelText('Nombre')).toBeInTheDocument()
    expect(screen.getByLabelText('Descripción')).toBeInTheDocument()
    expect(screen.getByLabelText('Fecha de la carrera')).toBeInTheDocument()

    expect(screen.queryByText('Estado')).toBeNull()
    expect(screen.queryByLabelText('Estado')).toBeNull()
    expect(screen.queryByRole('combobox')).toBeNull()
  })

  it('al guardar, el estado enviado es exactamente el actual de la carrera (nunca editable desde acá)', async () => {
    const user = userEvent.setup()
    const editedRace = race({ estado: 'EnCurso' })
    updateRace.mockResolvedValue(editedRace)
    renderWithProviders(<RaceFormModal race={editedRace} onClose={vi.fn()} onSaved={vi.fn()} />)

    await user.clear(screen.getByLabelText('Nombre'))
    await user.type(screen.getByLabelText('Nombre'), '5K Managua Centro')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(updateRace).toHaveBeenCalledTimes(1))
    const [raceId, payload] = updateRace.mock.calls[0]
    expect(raceId).toBe(editedRace.id)
    // El backend (RaceService.UpdateAsync) rechaza con 400 un Estado CAMBIADO —
    // Estado es derivado (RaceStatusDeriver) y solo POST /close ó /reopen pueden
    // moverlo. El formulario ya no ofrece ningún control para cambiarlo, pero
    // sigue reenviando el valor ACTUAL sin modificar: omitir la clave "estado"
    // del body haría que el binder de System.Text.Json la complete con el
    // default del enum (Planeada), y el guard del backend la rechazaría con 400
    // en cualquier carrera que no esté ya Planeada — un regreso real, no cosmético.
    expect(payload.estado).toBe(editedRace.estado)
  })
})
