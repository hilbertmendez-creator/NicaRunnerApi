import { MetricCard } from '@nicarunner/ui'

export function RaceMetrics() {
  return (
    <div className="flex gap-3 p-4">
      <MetricCard label="Inscritos" value={248} variant="gray" />
      <MetricCard label="Con tiempo" value={183} variant="teal" />
      <MetricCard label="Pendientes" value={65} variant="amber" />
    </div>
  )
}

export function Variants() {
  return (
    <div className="flex flex-col gap-2 p-4">
      <MetricCard label="Total inscritos" value={248} variant="gray" />
      <MetricCard label="En disputa" value={12} variant="orange" />
      <MetricCard label="Errores" value={3} variant="red" />
    </div>
  )
}

export function Sizes() {
  return (
    <div className="flex gap-3 p-4">
      <MetricCard label="Inscritos" value={248} variant="teal" size="md" />
      <MetricCard label="Inscritos" value={248} variant="teal" size="sm" />
    </div>
  )
}
