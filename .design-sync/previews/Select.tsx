import { Select } from '@nicarunner/ui'

export function RaceStatus() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <Select className="w-full" defaultValue="Planeada">
        <option value="Planeada">Planeada</option>
        <option value="EnCurso">En Curso</option>
        <option value="Terminada">Terminada</option>
      </Select>
    </div>
  )
}

export function CategorySelect() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <Select className="w-full" defaultValue="">
        <option value="" disabled>Seleccionar categoría...</option>
        <option value="1">Varonil Mayor (18-39)</option>
        <option value="2">Femenil Mayor (18-39)</option>
        <option value="3">Varonil Veterano (40+)</option>
        <option value="4">Femenil Veterano (40+)</option>
      </Select>
      <Select className="w-48" disabled>
        <option>Deshabilitado</option>
      </Select>
    </div>
  )
}
