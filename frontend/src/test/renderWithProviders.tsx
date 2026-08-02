import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, type RenderOptions } from '@testing-library/react'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type AuthContextValue, type CurrentUser } from '../auth/auth-context'
import { ThemeProvider } from '../hooks/useTheme'

export const adminUser: CurrentUser = {
  userId: 1,
  email: 'admin@test.local',
  nombre: 'Admin Test',
  role: 'Administrador',
  mustChangePassword: false,
}

export const lectorUser: CurrentUser = {
  ...adminUser,
  userId: 2,
  email: 'lector@test.local',
  nombre: 'Lector Test',
  role: 'Lector',
}

export function makeAuth(user: CurrentUser | null = adminUser): AuthContextValue {
  return {
    user,
    isAuthenticated: user != null,
    isLoading: false,
    login: async () => {},
    logout: () => {},
    clearMustChangePassword: () => {},
  }
}

interface ProvidersProps {
  children: ReactNode
  auth?: AuthContextValue
  initialPath?: string
  /** Rutas hijas bajo AppLayout Outlet cuando se monta el shell. */
  shellRoutes?: ReactNode
}

export function Providers({
  children,
  auth = makeAuth(),
  initialPath = '/',
}: ProvidersProps) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return (
    <QueryClientProvider client={client}>
      <AuthContext.Provider value={auth}>
        <ThemeProvider>
          <MemoryRouter initialEntries={[initialPath]}>{children}</MemoryRouter>
        </ThemeProvider>
      </AuthContext.Provider>
    </QueryClientProvider>
  )
}

export function renderWithProviders(
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'> & {
    auth?: AuthContextValue
    initialPath?: string
  },
) {
  const { auth, initialPath, ...rest } = options ?? {}
  return render(ui, {
    wrapper: ({ children }) => (
      <Providers auth={auth} initialPath={initialPath}>
        {children}
      </Providers>
    ),
    ...rest,
  })
}

/** Shell con AppLayout + rutas mínimas para smoke de nav/bell/badge. */
export function renderAppShell(
  AppLayout: React.ComponentType,
  options?: { auth?: AuthContextValue; initialPath?: string },
) {
  const initialPath = options?.initialPath ?? '/'
  return renderWithProviders(
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<div>Dashboard outlet</div>} />
        <Route path="/resultados" element={<div>Resultados outlet</div>} />
        <Route path="/controversias" element={<div>Controversias outlet</div>} />
        <Route path="/notificaciones" element={<div>Notificaciones outlet</div>} />
        <Route path="/enlaces" element={<div>Enlaces outlet</div>} />
      </Route>
    </Routes>,
    { auth: options?.auth, initialPath },
  )
}
