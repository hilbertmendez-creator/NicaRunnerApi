import { useState } from 'react'
import { Tabs } from '@nicarunner/ui'
import type { TabItem } from '@nicarunner/ui'

const RACE_TABS: TabItem[] = [
  { id: 'corredores', label: 'Corredores' },
  { id: 'categorias', label: 'Categorías' },
  { id: 'resultados', label: 'Resultados' },
]

export function RaceTabs() {
  const [active, setActive] = useState('corredores')
  return (
    <div className="p-4">
      <Tabs tabs={RACE_TABS} activeTab={active} onChange={setActive} />
    </div>
  )
}

export function TwoTabs() {
  const [active, setActive] = useState('resultados')
  const tabs: TabItem[] = [
    { id: 'resultados', label: 'Resultados' },
    { id: 'historial', label: 'Historial de auditoría' },
  ]
  return (
    <div className="p-4">
      <Tabs tabs={tabs} activeTab={active} onChange={setActive} />
    </div>
  )
}
