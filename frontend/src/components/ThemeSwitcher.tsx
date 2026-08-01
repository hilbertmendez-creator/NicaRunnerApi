import { useTheme } from '../hooks/useTheme'

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme()
  return (
    <div
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
          onClick={() => setTheme(t)}
          style={{
            height: 24,
            padding: '0 8px',
            borderRadius: 4,
            fontSize: 10,
            fontWeight: 500,
            border: 'none',
            cursor: 'pointer',
            fontFamily: 'Inter, system-ui',
            transition: 'all .12s',
            color: theme === t ? 'var(--tx-hi)' : 'var(--tx-lo)',
            background: theme === t ? 'var(--bg-card)' : 'transparent',
            boxShadow: theme === t ? 'var(--shadow-sm)' : 'none',
          }}
        >
          {t === 'light' ? 'Claro' : 'Oscuro'}
        </button>
      ))}
    </div>
  )
}
