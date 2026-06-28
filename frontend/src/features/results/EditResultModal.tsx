import { useState, type FormEvent } from 'react'
import type { ResultDto } from '../../api/types'
import { updateResult } from '../../api/endpoints'
import { Modal, Button, Label, Input, Textarea } from '@nicarunner/ui'

interface EditResultModalProps {
  raceId: number
  result: ResultDto
  onClose: () => void
  onSaved: () => void
}

function toLocalInputValue(iso: string) {
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

export function EditResultModal({ raceId, result, onClose, onSaved }: EditResultModalProps) {
  const [dorsal, setDorsal] = useState(result.dorsal ?? '')
  const [tiempoLlegada, setTiempoLlegada] = useState(toLocalInputValue(result.tiempoLlegada))
  const [razon, setRazon] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await updateResult(raceId, result.id, {
        dorsal,
        tiempoLlegada: new Date(tiempoLlegada).toISOString(),
        razon,
      })
      onSaved()
    } catch {
      setError('No se pudo guardar el cambio. Verifica los datos.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="edit-result-title">
      <form onSubmit={handleSubmit}>
        <h2 id="edit-result-title" className="mb-4 text-base font-semibold text-zinc-900">
          Editar resultado #{result.id}
        </h2>

        <Label htmlFor="result-dorsal">Dorsal</Label>
        <Input
          id="result-dorsal"
          value={dorsal}
          onChange={(e) => setDorsal(e.target.value)}
          required
          maxLength={20}
          className="mb-3 w-full"
        />

        <Label htmlFor="result-tiempo">Tiempo de llegada</Label>
        <Input
          id="result-tiempo"
          type="datetime-local"
          step="1"
          value={tiempoLlegada}
          onChange={(e) => setTiempoLlegada(e.target.value)}
          required
          className="mb-3 w-full"
        />

        <Label htmlFor="result-razon">Razón del cambio</Label>
        <Textarea
          id="result-razon"
          value={razon}
          onChange={(e) => setRazon(e.target.value)}
          required
          minLength={3}
          maxLength={300}
          rows={3}
          className="mb-3 w-full"
          placeholder="Ej: corrección por error de dorsal en captura"
        />

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
