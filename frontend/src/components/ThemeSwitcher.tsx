import { useTheme } from '../hooks/useTheme'

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme()
  const isDark = theme === 'dark'

  return (
    <button
      type="button"
      role="switch"
      aria-checked={isDark}
      aria-label="Modo oscuro"
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      className="theme-switch"
    >
      <span className="theme-switch-track" aria-hidden="true">
        <span className="theme-switch-cell theme-switch-cell-light" />
        <span className="theme-switch-cell theme-switch-cell-dark" />
        <span className={`theme-switch-plate ${isDark ? 'theme-switch-plate-dark' : ''}`} />
      </span>
      <span className="theme-switch-label hidden sm:inline" aria-hidden="true">
        {isDark ? 'Oscuro' : 'Claro'}
      </span>
    </button>
  )
}
