import { useCallback, useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getCategories, getRace } from '../../api/endpoints'
import type { RaceCategoryDto, RaceDto } from '../../api/types'
import { StatusBadge } from '../../components/StatusBadge'
import { useAuth } from '../../auth/auth-context'
import { CategoriesTab } from '../categories/CategoriesTab'
import { RunnersTab } from '../runners/RunnersTab'
import { RestartRaceDialog } from './RestartRaceDialog'
import { Button, Tabs } from '@nicarunner/ui'
import { pageTitle } from '../../theme/styles'

type Tab = 'categorias' | 'corredores'

export function RaceDetailPage() {
  const { raceId } = useParams<{ raceId: string }>()
  const id = Number(raceId)

  const [searchParams, setSearchParams] = useSearchParams()
  const initialTab: Tab = searchParams.get('tab') === 'corredores' ? 'corredores' : 'categorias'

  const { user } = useAuth()
  // Reiniciar una salida borra ceros y anula llegadas: Admin y nadie más, igual que en la
  // API. El botón no se muestra siquiera — no se ofrece lo que va a devolver 403.
  const isAdmin = user?.role === 'Administrador'

  const [race, setRace] = useState<RaceDto | null>(null)
  const [categories, setCategories] = useState<RaceCategoryDto[]>([])
  const [notFound, setNotFound] = useState(false)
  const [tab, setTab] = useState<Tab>(initialTab)
  const [showRestart, setShowRestart] = useState(false)

  const reload = useCallback(() => {
    getRace(id)
      .then(setRace)
      .catch(() => setNotFound(true))
    // Las categorías se piden acá y no dentro del tab porque el encabezado necesita saber
    // si hay alguna arrancada para decidir si ofrece reiniciar la salida.
    getCategories(id)
      .then(setCategories)
      .catch(() => setCategories([]))
  }, [id])

  useEffect(() => {
    if (!Number.isInteger(id)) {
      // One-time bail-out for an invalid route param, not a derived-state mirror.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setNotFound(true)
      return
    }
    reload()
  }, [id, reload])

  // Una categoría Planeada no tiene salida que reiniciar; una Terminada sí, porque la
  // salida en falso a veces se descubre después de que alguien ya la cerró.
  const arrancadas = categories.filter((cat) => cat.estado !== 'Planeada')

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
        <h1 className="text-lg font-semibold" style={pageTitle}>{race?.nombre ?? 'Cargando...'}</h1>
        {race && <StatusBadge status={race.estado} />}
        {isAdmin && arrancadas.length > 0 && (
          <Button
            variant="destructive"
            size="sm"
            className="ml-auto"
            onClick={() => setShowRestart(true)}
          >
            Reiniciar salida
          </Button>
        )}
      </div>

      {showRestart && (
        <RestartRaceDialog
          raceId={id}
          categories={arrancadas}
          onClose={() => setShowRestart(false)}
          onDone={(result) => {
            setShowRestart(false)
            toast.success(
              result.llegadasAnuladas === 1
                ? 'Salida reiniciada. Se anuló 1 llegada.'
                : `Salida reiniciada. Se anularon ${result.llegadasAnuladas} llegadas.`,
            )
            reload()
          }}
        />
      )}

      <Tabs
        tabs={tabItems}
        activeTab={tab}
        onChange={(val) => {
          setTab(val as Tab)
          setSearchParams(val === 'corredores' ? { tab: val } : {}, { replace: true })
        }}
      />

      <section
        id={`tabpanel-${tab}`}
        role="tabpanel"
        aria-labelledby={`tab-${tab}`}
        style={{ background: 'var(--bg-card)', border: '1px solid var(--bd-card)', borderRadius: 'var(--radius-card)', padding: 16 }}
      >
        {tab === 'categorias' ? <CategoriesTab raceId={id} /> : <RunnersTab raceId={id} />}
      </section>
    </div>
  )
}
