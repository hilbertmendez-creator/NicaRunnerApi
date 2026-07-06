import { Modal, Button, Label, Input, Select, ErrorAlert } from '@nicarunner/ui'

export function RaceForm() {
  return (
    <Modal onClose={() => {}}>
      <h2 className="mb-4 text-base font-semibold text-zinc-900">Nueva carrera</h2>
      <div className="flex flex-col gap-3">
        <div>
          <Label htmlFor="modal-nombre">Nombre</Label>
          <Input id="modal-nombre" defaultValue="Carrera de Montaña 2025" className="w-full" />
        </div>
        <div>
          <Label htmlFor="modal-fecha">Fecha de la carrera</Label>
          <Input id="modal-fecha" type="date" defaultValue="2025-09-14" className="w-full" />
        </div>
        <div>
          <Label htmlFor="modal-estado">Estado</Label>
          <Select id="modal-estado" className="w-full" defaultValue="Planeada">
            <option value="Planeada">Planeada</option>
            <option value="EnCurso">En Curso</option>
            <option value="Terminada">Terminada</option>
          </Select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary">Cancelar</Button>
          <Button variant="primary">Guardar</Button>
        </div>
      </div>
    </Modal>
  )
}

export function WithError() {
  return (
    <Modal onClose={() => {}}>
      <h2 className="mb-4 text-base font-semibold text-zinc-900">Nueva carrera</h2>
      <div className="flex flex-col gap-3">
        <div>
          <Label htmlFor="modal-nombre2">Nombre</Label>
          <Input id="modal-nombre2" defaultValue="" className="w-full" />
        </div>
        <ErrorAlert message="El nombre de la carrera es obligatorio." />
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary">Cancelar</Button>
          <Button variant="primary" disabled>Guardando...</Button>
        </div>
      </div>
    </Modal>
  )
}
