import type { KeyboardEvent } from 'react'

export interface TabItem {
  id: string
  label: string
}

interface TabsProps {
  tabs: TabItem[]
  activeTab: string
  onChange: (id: string) => void
  className?: string
}

export function Tabs({ tabs, activeTab, onChange, className = '' }: TabsProps) {
  function handleKeyDown(event: KeyboardEvent, index: number) {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return
    event.preventDefault()
    const delta = event.key === 'ArrowRight' ? 1 : -1
    const nextIndex = (index + delta + tabs.length) % tabs.length
    const nextTab = tabs[nextIndex]
    onChange(nextTab.id)
    document.getElementById(`tab-${nextTab.id}`)?.focus()
  }

  return (
    <div role="tablist" className={`flex gap-1 border-b border-zinc-200 ${className}`}>
      {tabs.map((tab, index) => {
        const isActive = tab.id === activeTab
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={isActive}
            aria-controls={`tabpanel-${tab.id}`}
            tabIndex={isActive ? 0 : -1}
            onClick={() => onChange(tab.id)}
            onKeyDown={(event) => handleKeyDown(event, index)}
            className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-700 focus-visible:ring-offset-1 ${
              isActive
                ? 'border-blue-700 text-blue-700'
                : 'border-transparent text-zinc-500 hover:border-zinc-300 hover:text-zinc-800'
            }`}
          >
            {tab.label}
          </button>
        )
      })}
    </div>
  )
}
