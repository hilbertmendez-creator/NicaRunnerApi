import { useMemo, useState, type FormEvent } from 'react'
import type { RaceCategoryDto, RunnerDto, Sexo } from '../../api/types'
import { createRunner, updateRunner } from '../../api/endpoints'
import { Modal, Button, Label, Input, Select } from '@nicarunner/ui'

interface RunnerFormModalProps {
  raceId: number
  runner: RunnerDto | null
  categories: RaceCategoryDto[]
  onClose: () => void
  onSaved: () => void
}

function toDateInputValue(iso?: string | null) {
  return iso ? iso.slice(0, 10) : ''
}

function calculateEdad(fechaNacimiento: string): number {
  const nacimiento = new Date(fechaNacimiento)
  const hoy = new Date()
  let edad = hoy.getFullYear() - nacimiento.getFullYear()
  const aunNoCumple =
    hoy.getMonth() < nacimiento.getMonth() ||
    (hoy.getMonth() === nacimiento.getMonth() && hoy.getDate() < nacimiento.getDate())
  if (aunNoCumple) edad--
  return Math.max(edad, 0)
}

export function RunnerFormModal({ raceId, runner, categories, onClose, onSaved }: RunnerFormModalProps) {
  const [nombre, setNombre] = useState(runner?.nombre ?? '')
  const [apellidos, setApellidos] = useState(runner?.apellidos ?? '')
  const [dorsal, setDorsal] = useState(runner?.dorsal ?? '')
  const [telefono, setTelefono] = useState(runner?.telefono ?? '')
  const [email, setEmail] = useState(runner?.email ?? '')
  const [sexo, setSexo] = useState<Sexo | ''>(runner?.sexo ?? '')
  const [club, setClub] = useState(runner?.club ?? '')
  const [fechaNacimiento, setFechaNacimiento] = useState(toDateInputValue(runner?.fechaNacimiento))
  const [edadManual, setEdadManual] = useState(runner?.edad ?? 18)
  const [categoryId, setCategoryId] = useState(runner?.categoryId ?? categories[0]?.categoryId ?? 0)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const edadCalculada = useMemo(
    () => (fechaNacimiento ? calculateEdad(fechaNacimiento) : null),
    [fechaNacimiento],
  )

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload = {
        nombre,
        apellidos: apellidos || null,
        dorsal,
        telefono: telefono || null,
        email: email || null,
        sexo: sexo || null,
        club: club || null,
        fechaNacimiento: fechaNacimiento ? new Date(fechaNacimiento).toISOString() : null,
        edad: edadCalculada ?? edadManual,
        categoryId,
      }
      if (runner) {
        await updateRunner(raceId, runner.id, payload)
      } else {
        await createRunner(raceId, payload)
      }
      onSaved()
    } catch {
      setError('No se pudo guardar el corredor. Verificá que el dorsal no esté duplicado y que la edad corresponda a la categoría.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="runner-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="runner-form-title" className="mb-4 text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
          {runner ? 'Editar corredor' : 'Nuevo corredor'}
        </h2>

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <Label htmlFor="runner-nombre">Nombre</Label>
            <Input
              id="runner-nombre"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              required
              maxLength={150}
              className="w-full"
            />
          </div>
          <div>
            <Label htmlFor="runner-apellidos">Apellidos</Label>
            <Input
              id="runner-apellidos"
              value={apellidos ?? ''}
              onChange={(e) => setApellidos(e.target.value)}
              maxLength={150}
              className="w-full"
            />
          </div>
        </div>

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
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
            <Label htmlFor="runner-sexo">Sexo</Label>
            <Select
              id="runner-sexo"
              value={sexo}
              onChange={(e) => setSexo(e.target.value as Sexo | '')}
              className="w-full"
            >
              <option value="">—</option>
              <option value="M">M</option>
              <option value="F">F</option>
            </Select>
          </div>
        </div>

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <Label htmlFor="runner-fecha-nacimiento">Fecha de nacimiento</Label>
            <Input
              id="runner-fecha-nacimiento"
              type="date"
              value={fechaNacimiento}
              onChange={(e) => setFechaNacimiento(e.target.value)}
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
              value={edadCalculada ?? edadManual}
              onChange={(e) => setEdadManual(Number(e.target.value))}
              disabled={edadCalculada !== null}
              required
              className="w-full"
            />
          </div>
        </div>

        <Label htmlFor="runner-club">Club</Label>
        <Input
          id="runner-club"
          value={club ?? ''}
          onChange={(e) => setClub(e.target.value)}
          maxLength={150}
          className="mb-3 w-full"
        />

        <Label htmlFor="runner-categoria">Categoría</Label>
        <Select
          id="runner-categoria"
          value={categoryId}
          onChange={(e) => setCategoryId(Number(e.target.value))}
          required
          className="mb-3 w-full"
        >
          {categories.map((cat) => (
            <option key={cat.categoryId} value={cat.categoryId}>
              {cat.nombreCategoria} ({cat.edadMinima}–{cat.edadMaxima})
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

        {error && <p className="mb-3 text-sm text-critical-600">{error}</p>}

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
