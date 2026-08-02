import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ErrorBoundary } from './components/ErrorBoundary'
import { ThemeProvider } from './hooks/useTheme'
import { ActiveRaceProvider } from './hooks/useActiveRace'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { RequirePasswordChanged } from './auth/RequirePasswordChanged'
import { AppLayout } from './components/AppLayout'
import { LoginPage } from './routes/LoginPage'
import { ForgotPasswordPage } from './routes/ForgotPasswordPage'
import { ResetPasswordPage } from './routes/ResetPasswordPage'
import { ChangePasswordPage } from './routes/ChangePasswordPage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { ResultsPage } from './features/results/ResultsPage'
import { RacesPage } from './features/races/RacesPage'
import { RaceDetailPage } from './features/races/RaceDetailPage'
import { NotificationsPage } from './features/notifications/NotificationsPage'
import { PublicLinksPage } from './features/public-links/PublicLinksPage'
import { PublicResultsPage } from './features/public-results/PublicResultsPage'
import { RunnerSharePage } from './features/public-results/RunnerSharePage'
import { UsersPage } from './features/users/UsersPage'
import { CategoryCatalogPage } from './features/categories/CategoryCatalogPage'

function App() {
  return (
    <ErrorBoundary>
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/resultados/:token" element={<PublicResultsPage />} />
          <Route path="/corredor/:shareKey" element={<RunnerSharePage />} />

          <Route element={<ProtectedRoute allowedRoles={['Administrador', 'Lector']} />}>
            <Route path="/change-password" element={<ChangePasswordPage />} />

            <Route element={<RequirePasswordChanged />}>
              <Route
                element={
                  <ActiveRaceProvider>
                    <AppLayout />
                  </ActiveRaceProvider>
                }
              >
                <Route path="/" element={<DashboardPage />} />
                <Route path="/carreras" element={<RacesPage />} />
                <Route path="/carreras/:raceId" element={<RaceDetailPage />} />
                <Route path="/resultados" element={<ResultsPage />} />
                <Route path="/notificaciones" element={<NotificationsPage />} />
                <Route path="/enlaces" element={<PublicLinksPage />} />

                <Route element={<ProtectedRoute allowedRoles={['Administrador']} />}>
                  <Route path="/usuarios" element={<UsersPage />} />
                  <Route path="/categorias" element={<CategoryCatalogPage />} />
                </Route>
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
    </ErrorBoundary>
  )
}

export default App
