import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { useRace } from '../../hooks/useRace'
import { createPublicToken, getPublicTokens } from '../../api/endpoints'
import type { PublicTokenDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, Label, Badge } from '@nicarunner/ui'
import { card, textLo, tableWrap, pageTitle } from '../../theme/styles'

function publicUrl(token: string) {
  return `${window.location.origin}/resultados/${token}`
}

// Validez preseteada (días). "Sin vencimiento" (0) requiere relajar el
// contrato backend `[Range(1,365)]` — se envía 0 y se nota como gap.
const PRESETS = [
  { label: '7 días', dias: 7 },
  { label: '30 días', dias: 30 },
  { label: '90 días', dias: 90 },
  { label: 'Sin vencimiento', dias: 0 },
] as const

export function PublicLinksPage() {
  const { user } = useAuth()
  const canCreate = user?.role === 'Administrador'

  const { raceId } = useRace()
  const [tokens, setTokens] = useState<PublicTokenDto[]>([])
  const [loading, setLoading] = useState(false)
  const [preset, setPreset] = useState(30)
  const [creating, setCreating] = useState(false)
  const [copiedId, setCopiedId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  function reload() {
    if (!raceId) return
    setLoading(true)
    getPublicTokens(raceId)
      .then(setTokens)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [raceId])

  async function handleCreate() {
    if (!raceId) return
    setError(null)
    setCreating(true)
    try {
      await createPublicToken(raceId, { diasExpiracion: preset })
      reload()
    } catch (err: any) {
      setError(err.response?.data?.detail ?? 'No se pudo generar el enlace público.')
    } finally {
      setCreating(false)
    }
  }

  async function handleCopy(token: PublicTokenDto) {
    await navigator.clipboard.writeText(publicUrl(token.token))
    setCopiedId(token.id)
    setTimeout(() => setCopiedId(null), 2000)
  }

  const isExpired = (fecha: string) => new Date(fecha) < new Date()

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-lg font-semibold" style={pageTitle}>Enlaces públicos de resultados</h1>
          <RaceSelector />
        </div>
      </div>

      {canCreate && (
        <section className="flex flex-wrap items-end gap-3" style={card}>
          <div>
            <Label>Días de validez</Label>
            <select
              className="h-8 border border-zinc-200 bg-white px-3 text-sm text-zinc-900 focus:border-blue-700 focus:outline-none"
              value={preset}
              onChange={(e) => setPreset(Number(e.target.value))}
              aria-label="Días de validez"
            >
              {PRESETS.map((p) => (
                <option key={p.dias} value={p.dias}>
                  {p.label}
                </option>
              ))}
            </select>
          </div>
          <Button variant="primary" onClick={handleCreate} disabled={!raceId || creating}>
            {creating ? 'Generando...' : 'Generar enlace'}
          </Button>
        </section>
      )}

      {error && <p className="text-sm" style={{ color: 'var(--badge-er-text)' }}>{error}</p>}

      {loading && <p className="text-sm" style={textLo}>Cargando enlaces...</p>}

      {!loading && raceId && tokens.length === 0 && (
        <p className="text-sm" style={textLo}>No hay enlaces públicos generados para esta carrera.</p>
      )}

      {tokens.length > 0 && (
        <section style={{ ...tableWrap, padding: 16 }}>
          <table className="w-full text-left text-sm">
            <thead>
              <tr style={{ color: 'var(--text-th)' }}>
                <th className="py-1">Enlace</th>
                <th className="py-1">Expira</th>
                <th className="py-1">Creado</th>
                <th className="py-1">Estado</th>
                <th className="py-1"></th>
              </tr>
            </thead>
            <tbody>
              {tokens.map((token) => (
                <tr key={token.id} style={{ borderTop: '1px solid var(--bd-row)' }}>
                  <td className="py-2 font-mono text-xs" style={{ color: 'var(--text-lo)' }}>{publicUrl(token.token)}</td>
                  <td className="py-2">
                    <span style={{ color: isExpired(token.fechaExpiracion) ? 'var(--badge-er-text)' : 'var(--text-lo)' }}>
                      {new Date(token.fechaExpiracion).toLocaleDateString('es-NI')}
                    </span>
                  </td>
                  <td className="py-2" style={{ color: 'var(--text-lo)' }}>{new Date(token.createdAt).toLocaleDateString('es-NI')}</td>
                  <td className="py-2">
                    {isExpired(token.fechaExpiracion) ? (
                      <Badge variant="neutral">Expirado</Badge>
                    ) : (
                      <Badge variant="success">Activo</Badge>
                    )}
                  </td>
                  <td className="py-2">
                    <Button size="sm" onClick={() => handleCopy(token)}>
                      {copiedId === token.id ? 'Copiado' : 'Copiar'}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </div>
  )
}