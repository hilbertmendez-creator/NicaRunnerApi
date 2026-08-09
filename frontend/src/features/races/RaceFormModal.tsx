import { useEffect, useState, type FormEvent } from 'react'
import { isAxiosError } from 'axios'
import toast from 'react-hot-toast'
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
  const [catalogLoading, setCatalogLoading] = useState(!race)
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  // Effect-driven fetch: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => {
    if (race) return
    let cancelled = false
    setCatalogLoading(true)
    getCategoryCatalog()
      .then((data) => {
        if (!cancelled) setCatalog(data)
      })
      .catch(() => {
        if (!cancelled) {
          setCatalog([])
          toast.error('No se pudo cargar el catálogo de categorías')
        }
      })
      .finally(() => {
        if (!cancelled) setCatalogLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [race])

  function toggleCategory(categoryId: number) {
    setSelectedCategoryIds((prev) =>
      prev.includes(categoryId) ? prev.filter((id) => id !== categoryId) : [...prev, categoryId],
    )
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setFieldErrors({})
    setSubmitting(true)
    try {
      const fechaIso = new Date(fechaCarrera).toISOString()
      if (race) {
        await updateRace(race.id, { nombre, descripcion, fechaCarrera: fechaIso, estado })
        toast.success('Carrera actualizada correctamente')
      } else {
        await createRace({ nombre, descripcion, fechaCarrera: fechaIso, categoryIds: selectedCategoryIds })
        toast.success('Carrera creada correctamente')
      }
      onSaved()
    } catch (err: unknown) {
      if (isAxiosError(err) && err.response?.data?.errors) {
        setFieldErrors(err.response.data.errors)
        toast.error('Verificá los errores en el formulario')
      } else {
        setError('No se pudo guardar la carrera. Verificá los datos.')
        toast.error('No se pudo guardar la carrera')
      }
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

        <Label htmlFor="race-nombre" required>Nombre</Label>
        <Input
          id="race-nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={150}
          placeholder="Ej: Maratón de León 2026"
          className="mb-1 w-full"
        />
        {fieldErrors['Nombre'] && (
          <p className="mb-3 text-sm text-critical-600">{fieldErrors['Nombre'].join(', ')}</p>
        )}
        {!fieldErrors['Nombre'] && <div className="mb-3" />}

        <Label htmlFor="race-descripcion">Descripción</Label>
        <Textarea
          id="race-descripcion"
          value={descripcion ?? ''}
          onChange={(e) => setDescripcion(e.target.value)}
          rows={2}
          placeholder="Detalles adicionales sobre la carrera (opcional)"
          className="mb-1 w-full"
        />
        {fieldErrors['Descripcion'] && (
          <p className="mb-3 text-sm text-critical-600">{fieldErrors['Descripcion'].join(', ')}</p>
        )}
        {!fieldErrors['Descripcion'] && <div className="mb-3" />}

        <Label htmlFor="race-fecha" required>Fecha de la carrera</Label>
        <Input
          id="race-fecha"
          type="date"
          value={fechaCarrera}
          onChange={(e) => setFechaCarrera(e.target.value)}
          required
          className="mb-1 w-full"
        />
        {fieldErrors['FechaCarrera'] && (
          <p className="mb-3 text-sm text-critical-600">{fieldErrors['FechaCarrera'].join(', ')}</p>
        )}
        {!fieldErrors['FechaCarrera'] && <div className="mb-3" />}

        {!race && (
          <div className="mb-3">
            <Label>Categorías participantes</Label>
            {catalogLoading ? (
              <div className="flex animate-pulse flex-col gap-2 pt-2" data-testid="category-catalog-skeleton">
                <div className="h-4 w-3/4 rounded bg-gray-200"></div>
                <div className="h-4 w-1/2 rounded bg-gray-200"></div>
                <div className="h-4 w-2/3 rounded bg-gray-200"></div>
              </div>
            ) : catalog.length === 0 ? (
              <p className="pt-2 text-sm" style={{ color: 'var(--text-lo)' }} data-testid="category-catalog-empty">
                No hay categorías en el catálogo.
              </p>
            ) : (
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
            )}
            {fieldErrors['CategoryIds'] && (
              <p className="mt-1 text-sm text-critical-600">{fieldErrors['CategoryIds'].join(', ')}</p>
            )}
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
