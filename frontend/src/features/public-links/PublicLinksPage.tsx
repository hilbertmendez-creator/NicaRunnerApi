import { useEffect, useState } from 'react'
import { createPublicToken, getPublicTokens, revokePublicToken } from '../../api/endpoints'
import type { PublicTokenDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { useActiveRace } from '../../hooks/useActiveRace'
import { Button, Label, Input, EmptyState } from '@nicarunner/ui'
import { pageTitle, card, textLo, tableWrap } from '../../theme/styles'

function publicUrl(token: string) {
  return `${window.location.origin}/resultados/${token}`
}

type LinkState = 'vigente' | 'vencido' | 'revocado'

// Tres estados, no dos. Un enlace revocado puede tener fecha futura, así que la
// fecha sola no alcanza para saber si sirve — y la revocación gana por encima
// de todo (es la lectura que hace ResolveValidTokenAsync en el backend).
function linkState(token: PublicTokenDto): LinkState {
  if (token.revocado) return 'revocado'
  return new Date(token.fechaExpiracion) < new Date() ? 'vencido' : 'vigente'
}

export function PublicLinksPage() {
  const { user } = useAuth()
  const canCreate = user?.role === 'Administrador'
  const { raceId } = useActiveRace()

  const [tokens, setTokens] = useState<PublicTokenDto[]>([])
  const [loading, setLoading] = useState(false)
  const [diasExpiracion, setDiasExpiracion] = useState(30)
  const [creating, setCreating] = useState(false)
  const [copiedId, setCopiedId] = useState<number | null>(null)
  const [revokingId, setRevokingId] = useState<number | null>(null)
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
      await createPublicToken(raceId, { diasExpiracion })
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

  async function handleRevoke(token: PublicTokenDto) {
    if (!raceId) return
    // Es irreversible y afecta a quien ya tenga el enlace: se confirma.
    const ok = window.confirm(
      'Revocar este enlace lo deja fuera de servicio de inmediato. ' +
        'Quien lo tenga guardado va a dejar de ver los resultados. ¿Continuar?',
    )
    if (!ok) return

    setError(null)
    setRevokingId(token.id)
    try {
      await revokePublicToken(raceId, token.id)
      reload()
    } catch (err: any) {
      setError(err.response?.data?.detail ?? 'No se pudo revocar el enlace público.')
    } finally {
      setRevokingId(null)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={pageTitle}>Enlaces públicos de resultados</h1>
      </div>

      {canCreate && (
        <section className="flex items-end gap-3" style={card}>
          <div>
            <Label htmlFor="dias-expiracion">Días de expiración</Label>
            <Input
              id="dias-expiracion"
              type="number"
              min="1"
              max="365"
              value={diasExpiracion}
              onChange={(e) => setDiasExpiracion(Number(e.target.value))}
              className="w-32"
            />
          </div>
          <Button variant="primary" onClick={handleCreate} disabled={!raceId || creating}>
            {creating ? 'Generando...' : 'Generar enlace'}
          </Button>
        </section>
      )}

      {error && (
        <p className="text-sm" role="alert" style={{ color: 'var(--badge-er-text)' }}>
          {error}
        </p>
      )}

      {loading && <p className="text-sm" style={textLo}>Cargando enlaces…</p>}

      {!loading && !raceId && (
        <EmptyState message="Elegí una carrera en la barra superior para gestionar enlaces públicos." />
      )}

      {!loading && raceId && tokens.length === 0 && (
        <EmptyState
          message={
            canCreate
              ? 'Todavía no hay enlaces públicos para esta carrera. Generá uno para compartir resultados.'
              : 'Todavía no hay enlaces públicos para esta carrera.'
          }
        />
      )}

      {tokens.length > 0 && (
        <section style={{ ...tableWrap, padding: 16 }}>
          <table className="w-full text-left text-sm">
            <thead>
              <tr style={{ color: 'var(--text-th)' }}>
                <th scope="col" className="py-1">Enlace</th>
                <th scope="col" className="py-1">Expira</th>
                <th scope="col" className="py-1">Creado</th>
                <th scope="col" className="py-1"></th>
              </tr>
            </thead>
            <tbody>
              {tokens.map((token) => {
                const estado = linkState(token)
                const activo = estado === 'vigente'
                return (
                  <tr key={token.id} style={{ borderTop: '1px solid var(--bd-row)' }}>
                    <td className="py-2 font-mono text-xs" style={{ color: 'var(--text-lo)' }}>{publicUrl(token.token)}</td>
                    <td className="py-2">
                      <span style={{ color: activo ? 'var(--text-lo)' : 'var(--badge-er-text)' }}>
                        {new Date(token.fechaExpiracion).toLocaleDateString('es-NI')}
                        {estado === 'vencido' && ' (vencido)'}
                        {estado === 'revocado' && ' (revocado)'}
                      </span>
                    </td>
                    <td className="py-2" style={{ color: 'var(--text-lo)' }}>{new Date(token.createdAt).toLocaleDateString('es-NI')}</td>
                    <td className="py-2">
                      {/* Copiar y revocar solo tienen sentido mientras el enlace
                          sirva: sobre uno muerto serían chrome que miente. */}
                      {activo && (
                        <div className="flex gap-2">
                          <Button size="sm" onClick={() => handleCopy(token)}>
                            {copiedId === token.id ? 'Copiado' : 'Copiar'}
                          </Button>
                          {canCreate && (
                            <Button
                              size="sm"
                              variant="destructive"
                              onClick={() => handleRevoke(token)}
                              disabled={revokingId === token.id}
                            >
                              {revokingId === token.id ? 'Revocando…' : 'Revocar'}
                            </Button>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </section>
      )}
    </div>
  )
}
