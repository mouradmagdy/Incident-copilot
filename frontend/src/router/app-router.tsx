import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../layout/app-layout'
import { OverviewPage } from '../pages/overview-page'
import { PlaceholderPage } from '../pages/placeholder-page'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<OverviewPage />} />
          <Route path="services" element={<PlaceholderPage />} />
          <Route path="logs" element={<PlaceholderPage />} />
          <Route path="incidents" element={<PlaceholderPage />} />
          <Route path="alerts" element={<PlaceholderPage />} />
          <Route path="infrastructure" element={<PlaceholderPage />} />
          <Route path="settings" element={<PlaceholderPage />} />
          <Route path="*" element={<PlaceholderPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
