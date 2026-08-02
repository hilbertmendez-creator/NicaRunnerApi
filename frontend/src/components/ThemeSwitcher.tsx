import { useTheme } from '../hooks/useTheme'

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme()
  return (
    <div
      role="group"
      aria-label="Tema"
      style={{
        display: 'flex',
        background: 'var(--bg-app)',
        border: '1px solid var(--bd)',
        borderRadius: 6,
        padding: 2,
        gap: 1,
      }}
    >
      {(['light', 'dark'] as const).map((t) => (
        <button
          key={t}
          type="button"
          className="nr-theme-btn"
          aria-pressed={theme === t}
          onClick={() => setTheme(t)}
          style={{
            height: 28,
            minWidth: 52,
            padding: '0 10px',
            borderRadius: 4,
            fontSize: 12,
            fontWeight: 500,
            border: 'none',
            cursor: 'pointer',
            fontFamily: 'Inter, system-ui',
            transition: 'background .12s, color .12s, box-shadow .12s',
            color: theme === t ? 'var(--tx-hi)' : 'var(--tx-lo)',
            background: theme === t ? 'var(--bg-card)' : 'transparent',
            boxShadow: theme === t ? 'var(--shadow-sm)' : 'none',
            outlineOffset: 2,
          }}
        >
          {t === 'light' ? 'Claro' : 'Oscuro'}
        </button>
      ))}
    </div>
  )
}
