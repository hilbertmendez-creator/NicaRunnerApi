import { createContext, useContext, useState, useEffect } from 'react'
import type { ReactNode } from 'react'

export type Theme = 'light' | 'dark'

interface ThemeCtx {
  theme: Theme
  setTheme: (t: Theme) => void
}

const STORAGE_KEY = 'nicarunner-theme'

const ThemeContext = createContext<ThemeCtx>({ theme: 'light', setTheme: () => {} })

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored === 'dark' ? 'dark' : 'light'
  })

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
  }, [theme])

  const setTheme = (t: Theme) => {
    setThemeState(t)
    localStorage.setItem(STORAGE_KEY, t)
  }

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

// Provider y hook conviven a propósito: es el patrón estándar de contexto en
// React y mantiene junto lo que se lee junto. Partirlo (como auth-context.ts)
// obligaría a retocar los vi.mock de varios tests, que no se chequean por tipos
// — un mock apuntando a la ruta vieja deja de mockear en silencio.
// El costo de dejarlo así es solo de DX: editar este archivo en dev dispara un
// full reload en vez de un hot update.
// eslint-disable-next-line react-refresh/only-export-components
export const useTheme = () => useContext(ThemeContext)
