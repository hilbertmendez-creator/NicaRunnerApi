import { useState, type FormEvent } from 'react'
import type { RaceCategoryDto, RunnerDto } from '../../api/types'
import { createRunner, updateRunner } from '../../api/endpoints'

interface RunnerFormModalProps {
  raceId: number
  runner: RunnerDto | null
  categories: RaceCategoryDto[]
  onClose: () => void
  onSaved: () => void
}

export function RunnerFormModal({ raceId, runner, categories, onClose, onSaved }: RunnerFormModalProps) {
  const [nombre, setNombre] = useState(runner?.nombre ?? '')
  const [dorsal, setDorsal] = useState(runner?.dorsal ?? '')
  const [telefono, setTelefono] = useState(runner?.telefono ?? '')
  const [email, setEmail] = useState(runner?.email ?? '')
  const [edad, setEdad] = useState(runner?.edad ?? 18)
  const [categoryId, setCategoryId] = useState(runner?.categoryId ?? categories[0]?.id ?? 0)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload = { nombre, dorsal, telefono, email, edad, categoryId }
      if (runner) {
        await updateRunner(raceId, runner.id, payload)
      } else {
        await createRunner(raceId, payload)
      }
      onSaved()
    } catch {
      setError('No se pudo guardar el corredor. Verifica que el dorsal no esté duplicado.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-base font-semibold text-gray-900">
          {runner ? 'Editar corredor' : 'Nuevo corredor'}
        </h2>

        <label className="mb-1 block text-sm font-medium text-gray-700">Nombre</label>
        <input
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={150}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Dorsal</label>
            <input
              value={dorsal}
              onChange={(e) => setDorsal(e.target.value)}
              required
              maxLength={20}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Edad</label>
            <input
              type="number"
              min="0"
              max="120"
              value={edad}
              onChange={(e) => setEdad(Number(e.target.value))}
              required
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
        </div>

        <label className="mb-1 block text-sm font-medium text-gray-700">Categoría</label>
        <select
          value={categoryId}
          onChange={(e) => setCategoryId(Number(e.target.value))}
          required
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        >
          {categories.map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.nombreCategoria}
            </option>
          ))}
        </select>

        <label className="mb-1 block text-sm font-medium text-gray-700">Teléfono</label>
        <input
          value={telefono ?? ''}
          onChange={(e) => setTelefono(e.target.value)}
          maxLength={20}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700">Email</label>
        <input
          type="email"
          value={email ?? ''}
          onChange={(e) => setEmail(e.target.value)}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

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
            disabled={submitting || categories.length === 0}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {submitting ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </div>
  )
}
