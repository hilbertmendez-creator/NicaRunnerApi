import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'

const SB_EXPANDED_KEY = 'nicarunner-sidebar-expanded'

// Mismo default que la referencia sidebar.js: colapsado en el primer acceso.
function loadExpanded(): boolean {
  return localStorage.getItem(SB_EXPANDED_KEY) === 'true'
}

export function AppLayout() {
  const [expanded, setExpanded] = useState(loadExpanded)
  const [mobileOpen, setMobileOpen] = useState(false)

  const toggleExpanded = () => {
    setExpanded((prev) => {
      const next = !prev
      localStorage.setItem(SB_EXPANDED_KEY, next ? 'true' : 'false')
      return next
    })
  }

  // Escape cierra el drawer móvil (referencia sidebar.js)
  useEffect(() => {
    if (!mobileOpen) return
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setMobileOpen(false)
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [mobileOpen])

  return (
    <div className={`shell-root${expanded ? '' : ' sb-collapsed'}`}>
      {/* Scrim (solo móvil/tablet, drawer abierto) */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden="true"
        />
      )}

      <Sidebar mobileOpen={mobileOpen} onClose={() => setMobileOpen(false)} />

      {/* Columna derecha: topbar + contenido */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Topbar
          collapsed={!expanded}
          onToggleCollapsed={toggleExpanded}
          onOpenMobile={() => setMobileOpen(true)}
        />
        <main className="p-4 sm:p-6" style={{ flex: 1, overflow: 'auto' }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}