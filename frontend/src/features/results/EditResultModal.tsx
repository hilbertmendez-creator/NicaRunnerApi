import { useState, type FormEvent } from 'react'
import type { ResultDto } from '../../api/types'
import { updateResult } from '../../api/endpoints'

interface EditResultModalProps {
  raceId: number
  result: ResultDto
  onClose: () => void
  onSaved: (updated: ResultDto) => void
}

function toLocalInputValue(iso: string) {
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

export function EditResultModal({ raceId, result, onClose, onSaved }: EditResultModalProps) {
  const [dorsal, setDorsal] = useState(result.dorsal)
  const [tiempoLlegada, setTiempoLlegada] = useState(toLocalInputValue(result.tiempoLlegada))
  const [razon, setRazon] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const updated = await updateResult(raceId, result.id, {
        dorsal,
        tiempoLlegada: new Date(tiempoLlegada).toISOString(),
        razon,
      })
      onSaved(updated)
    } catch {
      setError('No se pudo guardar el cambio. Verifica los datos.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md rounded-lg bg-white p-6 shadow-lg"
      >
        <h2 className="mb-4 text-base font-semibold text-gray-900">
          Editar resultado #{result.id}
        </h2>

        <label className="mb-1 block text-sm font-medium text-gray-700">Dorsal</label>
        <input
          value={dorsal}
          onChange={(e) => setDorsal(e.target.value)}
          required
          maxLength={20}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700">Tiempo de llegada</label>
        <input
          type="datetime-local"
          step="1"
          value={tiempoLlegada}
          onChange={(e) => setTiempoLlegada(e.target.value)}
          required
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700">Razón del cambio</label>
        <textarea
          value={razon}
          onChange={(e) => setRazon(e.target.value)}
          required
          minLength={3}
          maxLength={300}
          rows={3}
          className="mb-3 w-full rounded border border-gray-300 px-3 py-2 text-sm"
          placeholder="Ej: corrección por error de dorsal en captura"
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
