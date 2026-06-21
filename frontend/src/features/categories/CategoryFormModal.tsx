import { useState, type FormEvent } from 'react'
import type { RaceCategoryDto } from '../../api/types'
import { createCategory, updateCategory } from '../../api/endpoints'

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
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-base font-semibold text-gray-900">
          {category ? 'Editar categoría' : 'Nueva categoría'}
        </h2>

        <label className="mb-1 block text-sm font-medium text-gray-700">Nombre</label>
        <input
          value={nombreCategoria}
          onChange={(e) => setNombreCategoria(e.target.value)}
          required
          maxLength={100}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Distancia (km)</label>
            <input
              type="number"
              step="0.1"
              min="0.1"
              max="1000"
              value={distancia}
              onChange={(e) => setDistancia(Number(e.target.value))}
              required
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Orden</label>
            <input
              type="number"
              min="0"
              value={orden}
              onChange={(e) => setOrden(Number(e.target.value))}
              required
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
        </div>

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Edad mínima</label>
            <input
              type="number"
              min="0"
              max="120"
              value={edadMinima}
              onChange={(e) => setEdadMinima(Number(e.target.value))}
              required
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Edad máxima</label>
            <input
              type="number"
              min="0"
              max="120"
              value={edadMaxima}
              onChange={(e) => setEdadMaxima(Number(e.target.value))}
              required
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
        </div>

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {submitting ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </div>
  )
}
