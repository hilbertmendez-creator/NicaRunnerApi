import { formatElapsed } from './formatElapsed'
import type { PublicRunnerShareDto } from '../../api/types'

interface RunnerShareCardProps {
  data: PublicRunnerShareDto
}

function formatClockTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-NI', { hour12: false })
}

// Presentacional puro: solo props, sin fetching ni acceso al router (design.md
// Decisión 8). El discriminador de estado es tiempoLlegadaUtc — null significa
// "todavía no hay Result", nunca un valor de tiempo/posición centinela.
export function RunnerShareCard({ data }: RunnerShareCardProps) {
  const hasResult = data.tiempoLlegadaUtc !== null
  const elapsed = formatElapsed(data.tiempoTranscurridoSegundos)
  const hasCategoryPlacing = data.posicionCategoria !== null && data.totalCategoria !== null

  return (
    <div className="flex flex-col gap-4 p-5" style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)' }}>
      <div>
        <h1 className="text-lg font-semibold" style={{ color: 'var(--text-hi)' }}>{data.raceName}</h1>
        <p className="text-sm italic" style={{ color: 'var(--text-lo)' }}>{data.slogan}</p>
        <p className="mt-1 text-xs" style={{ color: 'var(--text-xs)' }}>
          {new Date(data.fechaCarrera).toLocaleDateString('es-NI')}
        </p>
      </div>

      <div className="flex flex-col gap-1">
        <p className="text-base font-semibold" style={{ color: 'var(--text-hi)' }}>
          {data.nombre}{data.apellidos ? ` ${data.apellidos}` : ''}
        </p>
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
          Dorsal {data.dorsal} · {data.nombreCategoria} ({data.distancia} km)
          {data.club ? ` · ${data.club}` : ''}
        </p>
      </div>

      {!hasResult && (
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>Sin tiempo registrado todavía.</p>
      )}

      {hasResult && (
        <div className="flex flex-col gap-2">
          {elapsed !== null ? (
            <p className="text-sm" style={{ color: 'var(--text-hi)' }}>
              Tiempo transcurrido: <span className="font-semibold">{elapsed}</span>
            </p>
          ) : (
            <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
              Hora de llegada: {formatClockTime(data.tiempoLlegadaUtc!)}
            </p>
          )}

          <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
            {hasCategoryPlacing
              ? `${data.posicionCategoria} de ${data.totalCategoria} en ${data.nombreCategoria}`
              : 'Categoría pendiente'}
          </p>

          {data.posicionGeneral !== null && data.totalGeneral !== null && (
            <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
              {data.posicionGeneral} de {data.totalGeneral} en general
            </p>
          )}
        </div>
      )}
    </div>
  )
}
