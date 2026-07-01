import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getRace } from '../../api/endpoints'
import type { RaceDto } from '../../api/types'
import { StatusBadge } from '../../components/StatusBadge'
import { CategoriesTab } from '../categories/CategoriesTab'
import { RunnersTab } from '../runners/RunnersTab'
import { Tabs } from '@nicarunner/ui'

type Tab = 'categorias' | 'corredores'

export function RaceDetailPage() {
  const { raceId } = useParams<{ raceId: string }>()
  const id = Number(raceId)

  const [race, setRace] = useState<RaceDto | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [tab, setTab] = useState<Tab>('categorias')

  useEffect(() => {
    if (!Number.isInteger(id)) {
      // One-time bail-out for an invalid route param, not a derived-state mirror.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setNotFound(true)
      return
    }
    getRace(id)
      .then(setRace)
      .catch(() => setNotFound(true))
  }, [id])

  if (notFound) {
    return (
      <div className="flex flex-col gap-3">
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>No se encontró la carrera solicitada.</p>
        <Link to="/carreras" className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
          ← Volver a carreras
        </Link>
      </div>
    )
  }

  const tabItems = [
    { id: 'categorias', label: 'Categorías' },
    { id: 'corredores', label: 'Corredores' },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div>
        <Link to="/carreras" className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
          ← Carreras
        </Link>
      </div>

      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold" style={{ color: 'var(--text-hi)' }}>{race?.nombre ?? 'Cargando...'}</h1>
        {race && <StatusBadge status={race.estado} />}
      </div>

      <Tabs tabs={tabItems} activeTab={tab} onChange={(val) => setTab(val as Tab)} />

      <section style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)', padding: 16 }}>
        {tab === 'categorias' ? <CategoriesTab raceId={id} /> : <RunnersTab raceId={id} />}
      </section>
    </div>
  )
}
