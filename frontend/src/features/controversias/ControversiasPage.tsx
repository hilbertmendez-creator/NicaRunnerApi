import { EmptyState } from '@nicarunner/ui'
import { pageTitle, textLo } from '../../theme/styles'

/**
 * Placeholder honesto para Slice A (nav honesty).
 * La grilla API-backed (DisputeResolutionGrid) llega en D3 — no montar mocks aquí.
 */
export function ControversiasPage() {
  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-lg font-semibold" style={pageTitle}>
        Controversias
      </h1>
      <p className="text-sm" style={textLo}>
        Resolución de tiempos multi-fuente. La API aún no está disponible.
      </p>
      <EmptyState message="Sin controversias que mostrar — pendiente de API (próximamente)." />
    </div>
  )
}
