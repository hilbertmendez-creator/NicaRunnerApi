import { Label, Input } from '@nicarunner/ui'

export function Standalone() {
  return (
    <div className="p-4">
      <Label>Nombre del corredor</Label>
      <Label>Categoría</Label>
      <Label>Dorsal</Label>
    </div>
  )
}

export function WithInput() {
  return (
    <div className="flex flex-col gap-3 p-4">
      <div>
        <Label htmlFor="name-preview">Nombre</Label>
        <Input id="name-preview" placeholder="Carlos Martínez" className="w-full" />
      </div>
      <div>
        <Label htmlFor="dorsal-preview">Dorsal</Label>
        <Input id="dorsal-preview" placeholder="101" className="w-32" />
      </div>
    </div>
  )
}
