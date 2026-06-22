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
        <p className="text-sm text-gray-700">No se encontró la carrera solicitada.</p>
        <Link to="/carreras" className="text-sm text-blue-700 hover:underline">
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
        <Link to="/carreras" className="text-sm text-blue-700 hover:underline">
          ← Carreras
        </Link>
      </div>

      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold text-zinc-900">{race?.nombre ?? 'Cargando...'}</h1>
        {race && <StatusBadge status={race.estado} />}
      </div>

      <Tabs tabs={tabItems} activeTab={tab} onChange={(val) => setTab(val as Tab)} />

      <section className="border border-zinc-200 bg-white p-4">
        {tab === 'categorias' ? <CategoriesTab raceId={id} /> : <RunnersTab raceId={id} />}
      </section>
    </div>
  )
}
