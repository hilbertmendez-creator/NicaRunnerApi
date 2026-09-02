import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RaceCategoryDto } from '../api/types'
import { RestartRaceDialog } from '../features/races/RestartRaceDialog'
import { renderWithProviders } from '../test/renderWithProviders'

const resetCategoryStart = vi.fn()
const restartRace = vi.fn()

vi.mock('../api/endpoints', () => ({
  resetCategoryStart: (...args: unknown[]) => resetCategoryStart(...args),
  restartRace: (...args: unknown[]) => restartRace(...args),
}))

function cat(categoryId: number, nombre: string): RaceCategoryDto {
  return {
    categoryId,
    codigo: `C${categoryId}`,
    nombreCategoria: nombre,
    distancia: 5,
    edadMinima: 18,
    edadMaxima: 99,
    orden: categoryId,
    estado: 'EnCurso',
    startUtc: '2026-09-01T14:00:00Z',
  }
}

/**
 * El reinicio total y el parcial NO son el mismo endpoint con distinta lista, y esa
 * diferencia es la que estos tests protegen: solo el total anula las capturas que todavía
 * no tienen dorsal, porque solo ahí se sabe que no pertenecen a otra categoría que sigue
 * corriendo bien.
 */
describe('RestartRaceDialog — el alcance elegido decide el endpoint', () => {
  beforeEach(() => {
    resetCategoryStart.mockReset()
    restartRace.mockReset()
    resetCategoryStart.mockResolvedValue({ categories: [], llegadasAnuladas: 2 })
    restartRace.mockResolvedValue({ categories: [], llegadasAnuladas: 5 })
  })

  function renderDialog(onDone = vi.fn()) {
    renderWithProviders(
      <RestartRaceDialog
        raceId={7}
        categories={[cat(3, 'Juvenil'), cat(7, 'Master')]}
        onClose={vi.fn()}
        onDone={onDone}
      />,
    )
    return onDone
  }

  it('con todas las categorías marcadas usa el reinicio de carrera completa', async () => {
    const onDone = renderDialog()
    await userEvent.type(screen.getByLabelText('Razón'), 'Salida en falso')
    await userEvent.click(screen.getByRole('button', { name: 'Reiniciar salida' }))

    await waitFor(() => expect(restartRace).toHaveBeenCalledWith(7, { razon: 'Salida en falso' }))
    expect(resetCategoryStart).not.toHaveBeenCalled()
    expect(onDone).toHaveBeenCalledWith({ categories: [], llegadasAnuladas: 5 })
  })

  it('desmarcando una categoría usa el reinicio parcial con las que quedaron', async () => {
    renderDialog()
    await userEvent.click(screen.getByRole('checkbox', { name: /Master/ }))
    await userEvent.type(screen.getByLabelText('Razón'), 'Solo Juvenil largó antes')
    await userEvent.click(screen.getByRole('button', { name: 'Reiniciar salida' }))

    await waitFor(() =>
      expect(resetCategoryStart).toHaveBeenCalledWith(7, {
        categoryIds: [3],
        razon: 'Solo Juvenil largó antes',
      }),
    )
    expect(restartRace).not.toHaveBeenCalled()
  })

  // La razón es obligatoria en la API. Bloquear el botón acá evita el viaje de ida y vuelta
  // para descubrirlo, sin sustituir la validación del servidor.
  it('sin razón escrita el botón queda deshabilitado', () => {
    renderDialog()
    expect(screen.getByRole('button', { name: 'Reiniciar salida' })).toBeDisabled()
  })

  it('sin ninguna categoría marcada el botón queda deshabilitado', async () => {
    renderDialog()
    await userEvent.type(screen.getByLabelText('Razón'), 'Salida en falso')
    await userEvent.click(screen.getByRole('checkbox', { name: /Juvenil/ }))
    await userEvent.click(screen.getByRole('checkbox', { name: /Master/ }))

    expect(screen.getByRole('button', { name: 'Reiniciar salida' })).toBeDisabled()
  })
})
