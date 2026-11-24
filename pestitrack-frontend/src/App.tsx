import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { Box, CircularProgress } from '@mui/material'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { FarmProvider } from './farms/FarmContext'
import { AppShell } from './components/AppShell'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { FieldsPage } from './pages/FieldsPage'
import { CropsPage } from './pages/CropsPage'
import { ProductsPage } from './pages/ProductsPage'
import { InventoryPage } from './pages/InventoryPage'
import { WorkersPage } from './pages/WorkersPage'
import { WeatherPage } from './pages/WeatherPage'
import { ApplicationsPage } from './pages/ApplicationsPage'
import { AdministrationPage } from './pages/AdministrationPage'

function ProtectedApp() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}>
        <CircularProgress aria-label="Loading session" />
      </Box>
    )
  }

  if (!user) return <Navigate to="/login" replace state={{ from: location }} />

  return (
    <FarmProvider>
      <AppShell />
    </FarmProvider>
  )
}

function AdministrationRoute() {
  const { isAdministrator } = useAuth()
  return isAdministrator ? <AdministrationPage /> : <Navigate to="/" replace />
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedApp />}>
          <Route index element={<DashboardPage />} />
          <Route path="fields" element={<FieldsPage />} />
          <Route path="crops" element={<CropsPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="workers" element={<WorkersPage />} />
          <Route path="weather" element={<WeatherPage />} />
          <Route path="inventory" element={<InventoryPage />} />
          <Route path="applications" element={<ApplicationsPage />} />
          <Route path="administration" element={<AdministrationRoute />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </AuthProvider>
  )
}
