import type { CSSProperties } from 'react'

/**
 * Estilos compartidos basados en los tokens de tema (index.css).
 * Usar estos en vez de clases Tailwind con colores hardcodeados para
 * que todo respete los 3 temas (dark / light / brand).
 */

export const pageTitle: CSSProperties = {
  color: 'var(--text-hi)',
}

export const textLo: CSSProperties = { color: 'var(--text-lo)' }
export const textXs: CSSProperties = { color: 'var(--text-xs)' }

export const card: CSSProperties = {
  background: 'var(--bg-card)',
  border: '1px solid var(--bd-card)',
  borderRadius: 'var(--radius-card)',
  padding: 16,
}

export const cardTitle: CSSProperties = {
  color: 'var(--text-hi)',
}

export const tableWrap: CSSProperties = {
  background: 'var(--bg-card)',
  border: '1px solid var(--bd-card)',
  borderRadius: 'var(--radius-card)',
  overflowX: 'auto',
}

export const badge = (bg: string, bd: string, text: string): CSSProperties => ({
  background: bg,
  border: `1px solid ${bd}`,
  color: text,
  borderRadius: 'var(--radius-badge)',
  padding: '2px 8px',
  fontSize: 11,
  fontWeight: 600,
})

/** Mini-métrica tipo tarjeta usada en resúmenes (Notificaciones). */
export const miniMetric = (accentText: string): CSSProperties => ({
  background: 'var(--bg-input)',
  border: '1px solid var(--bd)',
  borderRadius: 'var(--radius-card)',
  padding: 12,
  color: accentText,
})
