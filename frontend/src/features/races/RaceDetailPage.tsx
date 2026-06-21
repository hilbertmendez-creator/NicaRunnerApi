import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getRace } from '../../api/endpoints'
import type { RaceDto } from '../../api/types'
import { StatusBadge } from '../../components/StatusBadge'
import { CategoriesTab } from '../categories/CategoriesTab'
import { RunnersTab } from '../runners/RunnersTab'

type Tab = 'categorias' | 'corredores'

export function RaceDetailPage() {
  const { raceId } = useParams<{ raceId: string }>()
  const id = Number(raceId)

  const [race, setRace] = useState<RaceDto | null>(null)
  const [tab, setTab] = useState<Tab>('categorias')

  useEffect(() => {
    getRace(id).then(setRace)
  }, [id])

  return (
    <div className="flex flex-col gap-4">
      <div>
        <Link to="/carreras" className="text-sm text-blue-700 hover:underline">
          ← Carreras
        </Link>
      </div>

      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold text-gray-900">{race?.nombre ?? 'Cargando...'}</h1>
        {race && <StatusBadge status={race.estado} />}
      </div>

      <div className="flex gap-1 border-b border-gray-200">
        <button
          onClick={() => setTab('categorias')}
          className={`px-4 py-2 text-sm font-medium ${
            tab === 'categorias'
              ? 'border-b-2 border-blue-600 text-blue-700'
              : 'text-gray-500 hover:text-gray-800'
          }`}
        >
          Categorías
        </button>
        <button
          onClick={() => setTab('corredores')}
          className={`px-4 py-2 text-sm font-medium ${
            tab === 'corredores'
              ? 'border-b-2 border-blue-600 text-blue-700'
              : 'text-gray-500 hover:text-gray-800'
          }`}
        >
          Corredores
        </button>
      </div>

      <section className="rounded-lg bg-white p-4 shadow-sm">
        {tab === 'categorias' ? <CategoriesTab raceId={id} /> : <RunnersTab raceId={id} />}
      </section>
    </div>
  )
}
