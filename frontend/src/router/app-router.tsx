import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../layout/app-layout'
import { OverviewPage } from '../pages/overview-page'
import { PlaceholderPage } from '../pages/placeholder-page'
import { AuthPage } from '../pages/auth-page'
import { ProtectedRoute } from '../components/common/protected-route'
import { ServicesPage } from '../pages/services-page'
import { LogsPage } from '../pages/logs-page'
import { IncidentsPage } from '../pages/incidents-page'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="login" element={<AuthPage mode="login" />} />
        <Route path="signup" element={<AuthPage mode="signup" />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route index element={<OverviewPage />} />
            <Route path="services" element={<ServicesPage />} />
            <Route path="logs" element={<LogsPage />} />
            <Route path="incidents" element={<IncidentsPage />} />
            <Route path="*" element={<PlaceholderPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
