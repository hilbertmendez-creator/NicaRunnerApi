import { useState, type FormEvent } from 'react'
import type { RaceDto, RaceStatus } from '../../api/types'
import { createRace, updateRace } from '../../api/endpoints'

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
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const fechaIso = new Date(fechaCarrera).toISOString()
      if (race) {
        await updateRace(race.id, { nombre, descripcion, fechaCarrera: fechaIso, estado })
      } else {
        await createRace({ nombre, descripcion, fechaCarrera: fechaIso })
      }
      onSaved()
    } catch {
      setError('No se pudo guardar la carrera. Verifica los datos.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-base font-semibold text-gray-900">
          {race ? 'Editar carrera' : 'Nueva carrera'}
        </h2>

        <label className="mb-1 block text-sm font-medium text-gray-700">Nombre</label>
        <input
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={150}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700">Descripción</label>
        <textarea
          value={descripcion ?? ''}
          onChange={(e) => setDescripcion(e.target.value)}
          rows={2}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700">Fecha de la carrera</label>
        <input
          type="date"
          value={fechaCarrera}
          onChange={(e) => setFechaCarrera(e.target.value)}
          required
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        {race && (
          <>
            <label className="mb-1 block text-sm font-medium text-gray-700">Estado</label>
            <select
              value={estado}
              onChange={(e) => setEstado(e.target.value as RaceStatus)}
              className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
            >
              {ESTADOS.map((e) => (
                <option key={e} value={e}>
                  {e}
                </option>
              ))}
            </select>
          </>
        )}

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
