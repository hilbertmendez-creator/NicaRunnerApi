import { LoadingText } from '@nicarunner/ui'

export function Default() {
  return (
    <div className="p-4">
      <LoadingText />
    </div>
  )
}

export function CustomMessage() {
  return (
    <div className="p-4">
      <LoadingText message="Cargando dashboard..." />
    </div>
  )
}
