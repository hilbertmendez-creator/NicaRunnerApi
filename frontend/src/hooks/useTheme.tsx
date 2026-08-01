import { createContext, useContext, useState } from 'react'
import type { ReactNode } from 'react'

export type Theme = 'dark' | 'light'

interface ThemeCtx {
  theme: Theme
  setTheme: (t: Theme) => void
}

const STORAGE_KEY = 'nr_theme'
const VALID_THEMES: Theme[] = ['light', 'dark']
const DEFAULT_THEME: Theme = 'light'

function isTheme(value: unknown): value is Theme {
  return typeof value === 'string' && (VALID_THEMES as string[]).includes(value)
}

// Normalize-then-validate shim: remaps the retired 'brand' theme to 'light'
// and falls back to the default for absent, unknown, or corrupt values.
function normalizeTheme(raw: string | null): Theme {
  const remapped = raw === 'brand' ? 'light' : raw
  return isTheme(remapped) ? remapped : DEFAULT_THEME
}

// Reads and self-heals the stored theme. Read and write are guarded
// independently so a write-back failure never discards an already-resolved
// in-memory theme (e.g. quota exceeded, private-mode storage).
function readStoredTheme(): Theme {
  let theme: Theme
  try {
    theme = normalizeTheme(localStorage.getItem(STORAGE_KEY))
  } catch {
    return DEFAULT_THEME
  }
  try {
    localStorage.setItem(STORAGE_KEY, theme)
  } catch {
    // write-back failed — continue rendering with the resolved theme
  }
  return theme
}

const ThemeContext = createContext<ThemeCtx>({ theme: DEFAULT_THEME, setTheme: () => {} })

export function ThemeProvider({ children }: { children: ReactNode }) {
  // The pre-paint boot script in index.html already resolves data-theme
  // before first paint; read it first so React never re-derives a value
  // that disagrees with what is already on screen.
  const [theme, setThemeState] = useState<Theme>(() => {
    const domTheme = document.documentElement.dataset.theme
    if (isTheme(domTheme)) return domTheme
    const resolved = readStoredTheme()
    document.documentElement.dataset.theme = resolved
    return resolved
  })

  const setTheme = (t: Theme) => {
    document.documentElement.dataset.theme = t
    try {
      localStorage.setItem(STORAGE_KEY, t)
    } catch {
      // storage unavailable — DOM and in-memory state remain the source of truth
    }
    setThemeState(t)
  }

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

export const useTheme = () => useContext(ThemeContext)
