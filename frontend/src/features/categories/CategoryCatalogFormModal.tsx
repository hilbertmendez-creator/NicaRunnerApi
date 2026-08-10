import { useState, type FormEvent } from 'react'
import type { CategoryDto } from '../../api/types'
import { createCategoryCatalogEntry, updateCategoryCatalogEntry } from '../../api/endpoints'
import { Modal, Button, Label, Input, Textarea } from '@nicarunner/ui'

interface CategoryCatalogFormModalProps {
  category: CategoryDto | null
  onClose: () => void
  onSaved: () => void
}

export function CategoryCatalogFormModal({ category, onClose, onSaved }: CategoryCatalogFormModalProps) {
  const [codigo, setCodigo] = useState(category?.codigo ?? '')
  const [nombreCategoria, setNombreCategoria] = useState(category?.nombreCategoria ?? '')
  const [descripcion, setDescripcion] = useState(category?.descripcion ?? '')
  const [distancia, setDistancia] = useState(category?.distancia ?? 5)
  const [edadMinima, setEdadMinima] = useState(category?.edadMinima ?? 0)
  const [edadMaxima, setEdadMaxima] = useState(category?.edadMaxima ?? 120)
  const [orden, setOrden] = useState(category?.orden ?? 0)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (edadMinima >= edadMaxima) {
      setError('La edad mínima debe ser menor que la edad máxima.')
      return
    }

    setSubmitting(true)
    try {
      const payload = { codigo, nombreCategoria, descripcion, distancia, edadMinima, edadMaxima, orden }
      if (category) {
        await updateCategoryCatalogEntry(category.id, payload)
      } else {
        await createCategoryCatalogEntry(payload)
      }
      onSaved()
    } catch {
      setError('No se pudo guardar la categoría. Verificá que el código no esté repetido.')
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

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <Label htmlFor="cat-codigo" required>Código corto</Label>
            <Input
              id="cat-codigo"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
              required
              minLength={2}
              maxLength={10}
              pattern="[A-Za-z0-9]+"
              title="Alfanumérico, sin espacios ni símbolos"
              placeholder="Ej: JUV"
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="cat-orden" required>Orden</Label>
            <Input
              id="cat-orden"
              type="number"
              min="0"
              value={orden}
              onChange={(e) => setOrden(Number(e.target.value))}
              required
              placeholder="0"
              className="w-full"
            />
          </div>
        </div>

        <Label htmlFor="cat-nombre" required>Nombre</Label>
        <Input
          id="cat-nombre"
          value={nombreCategoria}
          onChange={(e) => setNombreCategoria(e.target.value)}
          required
          maxLength={100}
          placeholder="Ej: Juvenil"
          className="mb-3 w-full"
        />

        <Label htmlFor="cat-descripcion">Descripción</Label>
        <Textarea
          id="cat-descripcion"
          value={descripcion ?? ''}
          onChange={(e) => setDescripcion(e.target.value)}
          rows={2}
          maxLength={300}
          placeholder="Descripción breve de la categoría (opcional)"
          className="mb-3 w-full"
        />

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div>
            <Label htmlFor="cat-distancia" required>Distancia (km)</Label>
            <Input
              id="cat-distancia"
              type="number"
              step="0.1"
              min="0.1"
              max="1000"
              value={distancia}
              onChange={(e) => setDistancia(Number(e.target.value))}
              required
              placeholder="5.0"
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="cat-edad-min" required>Edad mínima</Label>
            <Input
              id="cat-edad-min"
              type="number"
              min="0"
              max="120"
              value={edadMinima}
              onChange={(e) => setEdadMinima(Number(e.target.value))}
              required
              placeholder="0"
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="cat-edad-max" required>Edad máxima</Label>
            <Input
              id="cat-edad-max"
              type="number"
              min="0"
              max="120"
              value={edadMaxima}
              onChange={(e) => setEdadMaxima(Number(e.target.value))}
              required
              placeholder="120"
              className="w-full"
            />
          </div>
        </div>

        {error && (
          <p className="mb-3 text-sm" role="alert" style={{ color: 'var(--er-tx)' }}>
            {error}
          </p>
        )}

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
