import { Textarea } from '@nicarunner/ui'

export function Empty() {
  return (
    <div className="p-4">
      <Textarea placeholder="Descripción de la carrera..." rows={3} className="w-full" />
    </div>
  )
}

export function WithContent() {
  return (
    <div className="p-4">
      <Textarea
        defaultValue="Carrera de montaña en la Sierra de Managua. Ruta de 42km con 1,200m de desnivel acumulado. Dirigida a corredores con experiencia en trail running."
        rows={4}
        className="w-full"
      />
    </div>
  )
}
