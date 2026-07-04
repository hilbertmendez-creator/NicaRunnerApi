import { useEffect, useState, type FormEvent } from 'react'
import type { CategoryDto, RaceDto, RaceStatus } from '../../api/types'
import { createRace, getCategoryCatalog, updateRace } from '../../api/endpoints'
import { Modal, Button, Label, Input, Textarea, Select } from '@nicarunner/ui'

interface RaceFormModalProps {
  race: RaceDto | null
  onClose: () => void
  onSaved: () => void
}

function toDateInputValue(iso: string) {
  return iso.slice(0, 10)
}

const ESTADOS: RaceStatus[] = ['Planeada', 'EnCurso', 'Terminada']

export function RaceFormModal({ race, onClose, onSaved }: RaceFormModalProps) {
  const [nombre, setNombre] = useState(race?.nombre ?? '')
  const [descripcion, setDescripcion] = useState(race?.descripcion ?? '')
  const [fechaCarrera, setFechaCarrera] = useState(
    race ? toDateInputValue(race.fechaCarrera) : '',
  )
  const [estado, setEstado] = useState<RaceStatus>(race?.estado ?? 'Planeada')
  const [catalog, setCatalog] = useState<CategoryDto[]>([])
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Effect-driven fetch: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => {
    if (race) return
    getCategoryCatalog().then(setCatalog)
  }, [race])

  function toggleCategory(categoryId: number) {
    setSelectedCategoryIds((prev) =>
      prev.includes(categoryId) ? prev.filter((id) => id !== categoryId) : [...prev, categoryId],
    )
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const fechaIso = new Date(fechaCarrera).toISOString()
      if (race) {
        await updateRace(race.id, { nombre, descripcion, fechaCarrera: fechaIso, estado })
      } else {
        await createRace({ nombre, descripcion, fechaCarrera: fechaIso, categoryIds: selectedCategoryIds })
      }
      onSaved()
    } catch {
      setError('No se pudo guardar la carrera. Verifica los datos.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="race-form-title">
      <form onSubmit={handleSubmit}>
        <h2
          id="race-form-title"
          className="mb-4 text-base font-semibold"
          style={{ color: 'var(--text-hi)' }}
        >
          {race ? 'Editar carrera' : 'Nueva carrera'}
        </h2>

        <Label htmlFor="race-nombre">Nombre</Label>
        <Input
          id="race-nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={150}
          className="mb-3 w-full"
        />

        <Label htmlFor="race-descripcion">Descripción</Label>
        <Textarea
          id="race-descripcion"
          value={descripcion ?? ''}
          onChange={(e) => setDescripcion(e.target.value)}
          rows={2}
          className="mb-3 w-full"
        />

        <Label htmlFor="race-fecha">Fecha de la carrera</Label>
        <Input
          id="race-fecha"
          type="date"
          value={fechaCarrera}
          onChange={(e) => setFechaCarrera(e.target.value)}
          required
          className="mb-3 w-full"
        />

        {!race && (
          <div className="mb-3">
            <Label>Categorías participantes</Label>
            {catalog.length === 0 && (
              <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
                No hay categorías en el catálogo todavía. Créalas primero en Administración → Categorías.
              </p>
            )}
            <div className="flex flex-col gap-1" style={{ maxHeight: 160, overflowY: 'auto' }}>
              {catalog.map((cat) => (
                <label key={cat.id} className="flex items-center gap-2 text-sm" style={{ color: 'var(--text-lo)' }}>
                  <input
                    type="checkbox"
                    checked={selectedCategoryIds.includes(cat.id)}
                    onChange={() => toggleCategory(cat.id)}
                  />
                  {cat.codigo} — {cat.nombreCategoria} ({cat.edadMinima}–{cat.edadMaxima})
                </label>
              ))}
            </div>
          </div>
        )}

        {race && (
          <>
            <Label htmlFor="race-estado">Estado</Label>
            <Select
              id="race-estado"
              value={estado}
              onChange={(e) => setEstado(e.target.value as RaceStatus)}
              className="mb-3 w-full"
            >
              {ESTADOS.map((e) => (
                <option key={e} value={e}>
                  {e}
                </option>
              ))}
            </Select>
          </>
        )}

        {error && <p className="mb-3 text-sm text-critical-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? 'Guardando...' : 'Guardar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
