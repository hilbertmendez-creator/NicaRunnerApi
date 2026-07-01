import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { createPublicToken, getPublicTokens } from '../../api/endpoints'
import type { PublicTokenDto } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'
import { pageTitle, card, textLo, tableWrap } from '../../theme/styles'

function publicUrl(token: string) {
  return `${window.location.origin}/resultados/${token}`
}

export function PublicLinksPage() {
  const { user } = useAuth()
  const canCreate = user?.role === 'Administrador'

  const [raceId, setRaceId] = useState<number | null>(null)
  const [tokens, setTokens] = useState<PublicTokenDto[]>([])
  const [loading, setLoading] = useState(false)
  const [diasExpiracion, setDiasExpiracion] = useState(30)
  const [creating, setCreating] = useState(false)
  const [copiedId, setCopiedId] = useState<number | null>(null)

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
    setCreating(true)
    try {
      await createPublicToken(raceId, { diasExpiracion })
      reload()
    } finally {
      setCreating(false)
    }
  }

  async function handleCopy(token: PublicTokenDto) {
    await navigator.clipboard.writeText(publicUrl(token.token))
    setCopiedId(token.id)
    setTimeout(() => setCopiedId(null), 2000)
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={pageTitle}>Enlaces públicos de resultados</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
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
                <th className="py-1"></th>
              </tr>
            </thead>
            <tbody>
              {tokens.map((token) => {
                const expired = new Date(token.fechaExpiracion) < new Date()
                return (
                  <tr key={token.id} style={{ borderTop: '1px solid var(--bd-row)' }}>
                    <td className="py-2 font-mono text-xs" style={{ color: 'var(--text-lo)' }}>{publicUrl(token.token)}</td>
                    <td className="py-2">
                      <span style={{ color: expired ? 'var(--badge-er-text)' : 'var(--text-lo)' }}>
                        {new Date(token.fechaExpiracion).toLocaleDateString('es-NI')}
                      </span>
                    </td>
                    <td className="py-2" style={{ color: 'var(--text-lo)' }}>{new Date(token.createdAt).toLocaleDateString('es-NI')}</td>
                    <td className="py-2">
                      <Button size="sm" onClick={() => handleCopy(token)}>
                        {copiedId === token.id ? 'Copiado' : 'Copiar'}
                      </Button>
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
