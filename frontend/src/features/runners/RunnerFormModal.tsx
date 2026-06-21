import { useState, type FormEvent } from 'react'
import type { RaceCategoryDto, RunnerDto } from '../../api/types'
import { createRunner, updateRunner } from '../../api/endpoints'
import { Modal } from '../../components/Modal'
import { Button } from '../../components/Button'
import { Label } from '../../components/form/Label'
import { Input } from '../../components/form/Input'
import { Select } from '../../components/form/Select'

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
    <Modal onClose={onClose} labelledBy="runner-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="runner-form-title" className="mb-4 text-base font-semibold text-gray-900">
          {runner ? 'Editar corredor' : 'Nuevo corredor'}
        </h2>

        <Label htmlFor="runner-nombre">Nombre</Label>
        <Input
          id="runner-nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={150}
          className="mb-3 w-full"
        />

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <Label htmlFor="runner-dorsal">Dorsal</Label>
            <Input
              id="runner-dorsal"
              value={dorsal}
              onChange={(e) => setDorsal(e.target.value)}
              required
              maxLength={20}
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="runner-edad">Edad</Label>
            <Input
              id="runner-edad"
              type="number"
              min="0"
              max="120"
              value={edad}
              onChange={(e) => setEdad(Number(e.target.value))}
              required
              className="w-full"
            />
          </div>
        </div>

        <Label htmlFor="runner-categoria">Categoría</Label>
        <Select
          id="runner-categoria"
          value={categoryId}
          onChange={(e) => setCategoryId(Number(e.target.value))}
          required
          className="mb-3 w-full"
        >
          {categories.map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.nombreCategoria}
            </option>
          ))}
        </Select>

        <Label htmlFor="runner-telefono">Teléfono</Label>
        <Input
          id="runner-telefono"
          value={telefono ?? ''}
          onChange={(e) => setTelefono(e.target.value)}
          maxLength={20}
          className="mb-3 w-full"
        />

        <Label htmlFor="runner-email">Email</Label>
        <Input
          id="runner-email"
          type="email"
          value={email ?? ''}
          onChange={(e) => setEmail(e.target.value)}
          className="mb-3 w-full"
        />

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={submitting || categories.length === 0}>
            {submitting ? 'Guardando...' : 'Guardar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
