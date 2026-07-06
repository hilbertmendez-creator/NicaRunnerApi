import { Button } from '@nicarunner/ui'

export function Primary() {
  return (
    <div className="flex gap-2 p-4">
      <Button variant="primary">Guardar</Button>
      <Button variant="primary" size="sm">Guardar</Button>
      <Button variant="primary" disabled>Guardando...</Button>
    </div>
  )
}

export function Secondary() {
  return (
    <div className="flex gap-2 p-4">
      <Button variant="secondary">Cancelar</Button>
      <Button variant="secondary" size="sm">Cancelar</Button>
      <Button variant="secondary" disabled>Cancelar</Button>
    </div>
  )
}

export function Destructive() {
  return (
    <div className="flex gap-2 p-4">
      <Button variant="destructive">Eliminar</Button>
      <Button variant="destructive" size="sm">Eliminar</Button>
    </div>
  )
}

export function Info() {
  return (
    <div className="flex gap-2 p-4">
      <Button variant="info">Ver detalles</Button>
      <Button variant="info" size="sm">Ver</Button>
    </div>
  )
}
