import { useEffect, useState } from 'react'
import { RaceSelector } from '../../components/RaceSelector'
import { createPublicToken, getPublicTokens } from '../../api/endpoints'
import type { PublicTokenDto } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'

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
        <h1 className="text-lg font-semibold text-gray-900">Enlaces públicos de resultados</h1>
        <RaceSelector value={raceId} onChange={setRaceId} />
      </div>

      {canCreate && (
        <section className="flex items-end gap-3 rounded-lg bg-white p-4 shadow-sm">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Días de expiración</label>
            <input
              type="number"
              min="1"
              max="365"
              value={diasExpiracion}
              onChange={(e) => setDiasExpiracion(Number(e.target.value))}
              className="w-32 rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <button
            onClick={handleCreate}
            disabled={!raceId || creating}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {creating ? 'Generando...' : 'Generar enlace'}
          </button>
        </section>
      )}

      {loading && <p className="text-sm text-gray-500">Cargando enlaces...</p>}

      {!loading && raceId && tokens.length === 0 && (
        <p className="text-sm text-gray-500">No hay enlaces públicos generados para esta carrera.</p>
      )}

      {tokens.length > 0 && (
        <section className="overflow-x-auto rounded-lg bg-white p-4 shadow-sm">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-gray-500">
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
                  <tr key={token.id} className="border-t border-gray-100">
                    <td className="py-2 font-mono text-xs text-gray-700">{publicUrl(token.token)}</td>
                    <td className="py-2">
                      <span className={expired ? 'text-red-600' : 'text-gray-700'}>
                        {new Date(token.fechaExpiracion).toLocaleDateString('es-NI')}
                      </span>
                    </td>
                    <td className="py-2">{new Date(token.createdAt).toLocaleDateString('es-NI')}</td>
                    <td className="py-2">
                      <button
                        onClick={() => handleCopy(token)}
                        className="rounded border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-100"
                      >
                        {copiedId === token.id ? 'Copiado' : 'Copiar'}
                      </button>
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
