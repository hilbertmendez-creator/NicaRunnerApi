import { EmptyState } from '@nicarunner/ui'

export function NoRaceSelected() {
  return (
    <div className="p-4">
      <EmptyState message="Selecciona una carrera para ver su progreso." />
    </div>
  )
}

export function NoResults() {
  return (
    <div className="p-4">
      <EmptyState message="Sin resultados capturados todavía." />
    </div>
  )
}
