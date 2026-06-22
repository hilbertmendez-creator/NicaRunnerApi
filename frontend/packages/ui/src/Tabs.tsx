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
  return (
    <div className={`flex gap-1 border-b border-zinc-200 ${className}`}>
      {tabs.map((tab) => {
        const isActive = tab.id === activeTab
        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => onChange(tab.id)}
            className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors duration-150 ${
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
