import { useState, type FormEvent } from 'react'
import type { RaceCategoryDto } from '../../api/types'
import { createCategory, updateCategory } from '../../api/endpoints'
import { Modal, Button, Label, Input } from '@nicarunner/ui'

interface CategoryFormModalProps {
  raceId: number
  category: RaceCategoryDto | null
  onClose: () => void
  onSaved: () => void
}

export function CategoryFormModal({ raceId, category, onClose, onSaved }: CategoryFormModalProps) {
  const [nombreCategoria, setNombreCategoria] = useState(category?.nombreCategoria ?? '')
  const [distancia, setDistancia] = useState(category?.distancia ?? 5)
  const [edadMinima, setEdadMinima] = useState(category?.edadMinima ?? 0)
  const [edadMaxima, setEdadMaxima] = useState(category?.edadMaxima ?? 120)
  const [orden, setOrden] = useState(category?.orden ?? 0)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload = { nombreCategoria, distancia, edadMinima, edadMaxima, orden }
      if (category) {
        await updateCategory(raceId, category.id, payload)
      } else {
        await createCategory(raceId, payload)
      }
      onSaved()
    } catch {
      setError('No se pudo guardar la categoría. Verifica los datos.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="category-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="category-form-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
          {category ? 'Editar categoría' : 'Nueva categoría'}
        </h2>

        <Label htmlFor="cat-nombre">Nombre</Label>
        <Input
          id="cat-nombre"
          value={nombreCategoria}
          onChange={(e) => setNombreCategoria(e.target.value)}
          required
          maxLength={100}
          className="mb-3 w-full"
        />

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <Label htmlFor="cat-distancia">Distancia (km)</Label>
            <Input
              id="cat-distancia"
              type="number"
              step="0.1"
              min="0.1"
              max="1000"
              value={distancia}
              onChange={(e) => setDistancia(Number(e.target.value))}
              required
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="cat-orden">Orden</Label>
            <Input
              id="cat-orden"
              type="number"
              min="0"
              value={orden}
              onChange={(e) => setOrden(Number(e.target.value))}
              required
              className="w-full"
            />
          </div>
        </div>

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <Label htmlFor="cat-edad-min">Edad mínima</Label>
            <Input
              id="cat-edad-min"
              type="number"
              min="0"
              max="120"
              value={edadMinima}
              onChange={(e) => setEdadMinima(Number(e.target.value))}
              required
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="cat-edad-max">Edad máxima</Label>
            <Input
              id="cat-edad-max"
              type="number"
              min="0"
              max="120"
              value={edadMaxima}
              onChange={(e) => setEdadMaxima(Number(e.target.value))}
              required
              className="w-full"
            />
          </div>
        </div>

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
