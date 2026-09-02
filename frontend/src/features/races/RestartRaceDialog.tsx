import { useState } from 'react'
import { resetCategoryStart, restartRace } from '../../api/endpoints'
import { apiErrorMessage } from '../../api/client'
import type { RaceCategoryDto, ResetStartResultDto } from '../../api/types'
import { Button, Label, Modal, Textarea } from '@nicarunner/ui'

const RAZON_MINIMA = 3

/**
 * Reinicio por salida en falso.
 *
 * Es la única acción del backoffice que borra el cero de una categoría y anula las llegadas
 * medidas contra él, así que el diálogo está construido para que nadie la ejecute por
 * accidente ni sin saber qué se lleva: nombra las categorías afectadas una por una, exige
 * una razón escrita, y el botón es destructivo. No hay "deshacer" del otro lado.
 */
export function RestartRaceDialog({
  raceId,
  categories,
  onClose,
  onDone,
}: {
  raceId: number
  /** Solo las que realmente arrancaron: una Planeada no tiene salida que reiniciar. */
  categories: RaceCategoryDto[]
  onClose: () => void
  onDone: (result: ResetStartResultDto) => void
}) {
  // Arranca con todas marcadas porque el caso típico de una salida en falso es el balazo
  // completo; desmarcar es el caso raro (una sola categoría se largó antes).
  const [selected, setSelected] = useState<number[]>(() => categories.map((c) => c.categoryId))
  const [razon, setRazon] = useState('')
  const [working, setWorking] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const todas = selected.length === categories.length
  const puedeReiniciar = selected.length > 0 && razon.trim().length >= RAZON_MINIMA && !working

  function toggle(categoryId: number) {
    setSelected((prev) =>
      prev.includes(categoryId) ? prev.filter((id) => id !== categoryId) : [...prev, categoryId],
    )
  }

  async function handleSubmit() {
    if (!puedeReiniciar) return
    setError(null)
    setWorking(true)
    try {
      // Dos endpoints y no uno con "todas las categorías": el reinicio total también
      // alcanza las capturas que todavía no tienen dorsal, y eso solo es correcto cuando
      // no queda ninguna otra categoría a la que pudieran pertenecer.
      const result = todas
        ? await restartRace(raceId, { razon: razon.trim() })
        : await resetCategoryStart(raceId, { categoryIds: selected, razon: razon.trim() })
      onDone(result)
    } catch (err) {
      setError(apiErrorMessage(err, 'No se pudo reiniciar la salida.'))
    } finally {
      setWorking(false)
    }
  }

  return (
    <Modal onClose={onClose} maxWidth="lg" labelledBy="restart-race-title">
      <div className="flex flex-col gap-4">
        <div>
          <h2 id="restart-race-title" className="text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
            Reiniciar la salida
          </h2>
          <p className="mt-1 text-sm" style={{ color: 'var(--text-lo)' }}>
            Las categorías que elijas vuelven a Planeada, pierden su hora de salida y se anulan
            todas las llegadas registradas contra ella. Después habrá que volver a dar el cero.
          </p>
        </div>

        <fieldset className="flex flex-col gap-2">
          <legend className="text-sm font-medium" style={{ color: 'var(--text-hi)' }}>
            Categorías a reiniciar
          </legend>
          {categories.map((cat) => (
            <label key={cat.categoryId} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={selected.includes(cat.categoryId)}
                onChange={() => toggle(cat.categoryId)}
              />
              <span style={{ color: 'var(--text-hi)' }}>{cat.nombreCategoria}</span>
              <span style={{ color: 'var(--text-lo)' }}>
                {cat.estado === 'Terminada' ? 'cerrada' : 'en curso'}
                {cat.startUtc ? ` · salió ${new Date(cat.startUtc).toLocaleTimeString()}` : ''}
              </span>
            </label>
          ))}
        </fieldset>

        <div className="flex flex-col gap-1">
          <Label htmlFor="restart-razon">Razón</Label>
          <Textarea
            id="restart-razon"
            rows={2}
            value={razon}
            onChange={(e) => setRazon(e.target.value)}
            placeholder="Ej.: salida en falso, el pelotón largó antes del disparo."
          />
          <p className="text-xs" style={{ color: 'var(--text-lo)' }}>
            Queda en la bitácora y en el historial de cada llegada anulada.
          </p>
        </div>

        {error && (
          <p className="text-sm" role="alert" style={{ color: 'var(--badge-er-text)' }}>
            {error}
          </p>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose} disabled={working}>
            Cancelar
          </Button>
          <Button variant="destructive" onClick={handleSubmit} disabled={!puedeReiniciar}>
            {working ? 'Reiniciando...' : 'Reiniciar salida'}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
