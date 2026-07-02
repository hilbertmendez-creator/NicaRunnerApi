import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { RequirePasswordChanged } from './auth/RequirePasswordChanged'
import { AppLayout } from './components/AppLayout'
import { LoginPage } from './routes/LoginPage'
import { ChangePasswordPage } from './routes/ChangePasswordPage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { ResultsPage } from './features/results/ResultsPage'
import { RacesPage } from './features/races/RacesPage'
import { RaceDetailPage } from './features/races/RaceDetailPage'
import { NotificationsPage } from './features/notifications/NotificationsPage'
import { PublicLinksPage } from './features/public-links/PublicLinksPage'
import { PublicResultsPage } from './features/public-results/PublicResultsPage'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/resultados/:token" element={<PublicResultsPage />} />

          <Route element={<ProtectedRoute allowedRoles={['Administrador', 'Lector']} />}>
            <Route path="/change-password" element={<ChangePasswordPage />} />

            <Route element={<RequirePasswordChanged />}>
              <Route element={<AppLayout />}>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/carreras" element={<RacesPage />} />
                <Route path="/carreras/:raceId" element={<RaceDetailPage />} />
                <Route path="/resultados" element={<ResultsPage />} />
                <Route path="/notificaciones" element={<NotificationsPage />} />
                <Route path="/enlaces" element={<PublicLinksPage />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
