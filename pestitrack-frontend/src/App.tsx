import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { Box, CircularProgress } from '@mui/material'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { FarmProvider } from './farms/FarmContext'
import { AppShell } from './components/AppShell'

const DashboardPage = lazy(() => import('./pages/DashboardPage').then((module) => ({ default: module.DashboardPage })))
const LoginPage = lazy(() => import('./pages/LoginPage').then((module) => ({ default: module.LoginPage })))
const FieldsPage = lazy(() => import('./pages/FieldsPage').then((module) => ({ default: module.FieldsPage })))
const PlotsPage = lazy(() => import('./pages/PlotsPage').then((module) => ({ default: module.PlotsPage })))
const CropsPage = lazy(() => import('./pages/CropsPage').then((module) => ({ default: module.CropsPage })))
const ProductsPage = lazy(() => import('./pages/ProductsPage').then((module) => ({ default: module.ProductsPage })))
const InventoryPage = lazy(() => import('./pages/InventoryPage').then((module) => ({ default: module.InventoryPage })))
const WorkersPage = lazy(() => import('./pages/WorkersPage').then((module) => ({ default: module.WorkersPage })))

const ApplicationsPage = lazy(() => import('./pages/ApplicationsPage').then((module) => ({ default: module.ApplicationsPage })))
const AdministrationPage = lazy(() => import('./pages/AdministrationPage').then((module) => ({ default: module.AdministrationPage })))
const ApplicationHistoryPage = lazy(() => import('./pages/ApplicationHistoryPage').then((module) => ({ default: module.ApplicationHistoryPage })))
const ReportsPage = lazy(() => import('./pages/ReportsPage').then((module) => ({ default: module.ReportsPage })))
const UserAdministrationPage = lazy(() => import('./pages/UserAdministrationPage').then((module) => ({ default: module.UserAdministrationPage })))
const AuditPage = lazy(() => import('./pages/AuditPage').then((module) => ({ default: module.AuditPage })))
const CompliancePage = lazy(() => import('./pages/CompliancePage').then((module) => ({ default: module.CompliancePage })))
const NotificationsPage = lazy(() => import('./pages/NotificationsPage').then((module) => ({ default: module.NotificationsPage })))
const AccountPage = lazy(() => import('./pages/AccountPage').then((module) => ({ default: module.AccountPage })))
const FarmsPage = lazy(() => import('./pages/FarmsPage').then((module) => ({ default: module.FarmsPage })))

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

function UserAdministrationRoute() {
  const { isAdministrator } = useAuth()
  return isAdministrator ? <UserAdministrationPage /> : <Navigate to="/" replace />
}

function FarmsRoute() {
  const { isAdministrator } = useAuth()
  return isAdministrator ? <FarmsPage /> : <Navigate to="/" replace />
}

export default function App() {
  return (
    <AuthProvider>
      <Suspense fallback={<Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<ProtectedApp />}>
            <Route index element={<DashboardPage />} />
            <Route path="fields" element={<FieldsPage />} />
            <Route path="plots" element={<PlotsPage />} />
            <Route path="crops" element={<CropsPage />} />
            <Route path="products" element={<ProductsPage />} />
            <Route path="workers" element={<WorkersPage />} />
            <Route path="inventory" element={<InventoryPage />} />
            <Route path="applications" element={<ApplicationsPage />} />
            <Route path="application-history" element={<ApplicationHistoryPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="compliance" element={<CompliancePage />} />
            <Route path="notifications" element={<NotificationsPage />} />
            <Route path="audit" element={<AuditPage />} />
            <Route path="account" element={<AccountPage />} />
            <Route path="administration" element={<AdministrationRoute />} />
            <Route path="users" element={<UserAdministrationRoute />} />
            <Route path="farms" element={<FarmsRoute />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </Suspense>
    </AuthProvider>
  )
}
