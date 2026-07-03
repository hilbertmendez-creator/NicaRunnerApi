import { useTheme } from '../hooks/useTheme'
import type { Theme } from '../hooks/useTheme'

const THEMES: { value: Theme; label: string }[] = [
  { value: 'dark', label: '⬛ Control' },
  { value: 'light', label: '☀ Limpio' },
  { value: 'brand', label: '◆ Marca' },
]

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme()
  return (
    <div
      style={{
        display: 'flex',
        gap: 2,
        padding: 3,
        background: 'var(--bg-app)',
        border: '1px solid var(--bd)',
        borderRadius: 9,
      }}
    >
      {THEMES.map((t) => (
        <button
          key={t.value}
          type="button"
          onClick={() => setTheme(t.value)}
          style={{
            padding: '4px 9px',
            fontSize: 10.5,
            fontWeight: theme === t.value ? 600 : 400,
            color: theme === t.value ? 'var(--accent)' : 'var(--text-xs)',
            background: theme === t.value ? 'var(--accent-bg)' : 'transparent',
            border: 'none',
            borderRadius: 6,
            cursor: 'pointer',
            fontFamily: 'Inter, system-ui',
            transition: 'all .15s',
          }}
        >
          {t.label}
        </button>
      ))}
    </div>
  )
}
