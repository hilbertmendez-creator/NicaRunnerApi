// design.md Decisión 5: TS recibe segundos enteros (nunca un TimeSpan .NET
// pre-formateado ni un string con el locale ya decidido en el backend) y decide acá,
// una sola vez, cómo mostrarlo. H:MM:SS, sin el segmento de horas cuando es cero —
// "42:17", no "0:42:17". null significa "sin tiempo transcurrido" y se propaga tal cual.
export function formatElapsed(totalSeconds: number | null): string | null {
  if (totalSeconds === null) return null

  const safeSeconds = Math.max(0, Math.trunc(totalSeconds))
  const hours = Math.floor(safeSeconds / 3600)
  const minutes = Math.floor((safeSeconds % 3600) / 60)
  const seconds = safeSeconds % 60

  const mm = String(minutes).padStart(2, '0')
  const ss = String(seconds).padStart(2, '0')

  return hours > 0 ? `${hours}:${mm}:${ss}` : `${minutes}:${ss}`
}
