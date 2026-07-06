import { Input } from '@nicarunner/ui'

export function Empty() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <Input placeholder="Ingresa el nombre del corredor" className="w-full" />
    </div>
  )
}

export function WithValue() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <Input defaultValue="Carlos Martínez" className="w-full" />
      <Input defaultValue="101" className="w-32" />
    </div>
  )
}

export function States() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <Input placeholder="Campo normal" className="w-full" />
      <Input placeholder="Campo requerido" required className="w-full" />
      <Input defaultValue="Solo lectura" readOnly className="w-full" />
      <Input placeholder="Deshabilitado" disabled className="w-full" />
    </div>
  )
}
