import { useState, type FormEvent } from 'react'
import type { RaceDto, RaceStatus } from '../../api/types'
import { createRace, updateRace } from '../../api/endpoints'
import { Modal } from '../../components/Modal'
import { Button } from '../../components/Button'
import { Label } from '../../components/form/Label'
import { Input } from '../../components/form/Input'
import { Textarea } from '../../components/form/Textarea'
import { Select } from '../../components/form/Select'

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
    <Modal onClose={onClose} labelledBy="race-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="race-form-title" className="mb-4 text-base font-semibold text-gray-900">
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

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

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
