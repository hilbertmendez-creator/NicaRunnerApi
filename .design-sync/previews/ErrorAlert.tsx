import { ErrorAlert } from '@nicarunner/ui'

export function NetworkError() {
  return (
    <div className="p-4">
      <ErrorAlert message="No se pudo conectar al servidor. Verifica tu conexión." />
    </div>
  )
}

export function ValidationError() {
  return (
    <div className="p-4">
      <ErrorAlert message="El dorsal ingresado no existe en esta carrera." />
    </div>
  )
}

export function SaveError() {
  return (
    <div className="p-4">
      <ErrorAlert message="No se pudo guardar la carrera. Verifica los datos e intenta de nuevo." />
    </div>
  )
}
