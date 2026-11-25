import { Link as RouterLink } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import AppsRoundedIcon from '@mui/icons-material/AppsRounded'
import GrassRoundedIcon from '@mui/icons-material/GrassRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded'
import { useAuth } from '../auth/AuthContext'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { FarmDashboard, SystemDashboard } from '../types/api'

const shortcuts = [
  {
    title: 'Fields and crops',
    description: 'Plan crop cycles and manage every field.',
    path: '/fields',
    permission: 'fields.view',
    icon: <GrassRoundedIcon />,
  },
  {
    title: 'Inventory',
    description: 'Review balances, receipts, issues, and adjustments.',
    path: '/inventory',
    permission: 'inventory.view',
    icon: <Inventory2RoundedIcon />,
  },
  {
    title: 'Applications',
    description: 'Draft and complete pesticide applications.',
    path: '/applications',
    permission: 'applications.view',
    icon: <AppsRoundedIcon />,
  },
  {
    title: 'Workers',
    description: 'Manage workers and applicator certifications.',
    path: '/workers',
    permission: 'workers.view',
    icon: <PeopleRoundedIcon />,
  },
]

export function DashboardPage() {
  const { user, isAdministrator } = useAuth()
  const { selectedFarm, farms, isLoading, error, hasPermission } = useFarm()
  const farmDashboard = useQuery({
    queryKey: ['dashboard', selectedFarm?.id],
    queryFn: () => apiRequest<FarmDashboard>(`/api/farms/${selectedFarm?.id}/dashboard`),
    enabled: Boolean(selectedFarm) && hasPermission('farms.view'),
    retry: false,
  })
  const systemDashboard = useQuery({
    queryKey: ['system-dashboard'],
    queryFn: () => apiRequest<SystemDashboard>('/api/dashboard'),
    enabled: isAdministrator,
    retry: false,
  })
  const dashboardError = farmDashboard.error ?? systemDashboard.error
  const visibleShortcuts = shortcuts.filter((shortcut) => hasPermission(shortcut.permission))

  const alerts: Array<{ severity: 'error' | 'warning'; message: string; path: string; action: string }> = []
  if (farmDashboard.data?.workers) {
    if (farmDashboard.data.workers.expiredCertifications > 0) {
      alerts.push({
        severity: 'error',
        message: `${farmDashboard.data.workers.expiredCertifications} worker certification(s) have expired.`,
        path: '/workers',
        action: 'Review workers',
      })
    }
    if (farmDashboard.data.workers.certificationsExpiringWithin30Days > 0) {
      alerts.push({
        severity: 'warning',
        message: `${farmDashboard.data.workers.certificationsExpiringWithin30Days} worker certification(s) expire within 30 days.`,
        path: '/workers',
        action: 'Review workers',
      })
    }
  }
  if (farmDashboard.data?.inventory) {
    if (farmDashboard.data.inventory.expiredBatchCount > 0) {
      alerts.push({
        severity: 'error',
        message: `${farmDashboard.data.inventory.expiredBatchCount} stocked inventory batch(es) have expired.`,
        path: '/inventory',
        action: 'Review inventory',
      })
    }
    if (farmDashboard.data.inventory.batchesExpiringWithin30Days > 0) {
      alerts.push({
        severity: 'warning',
        message: `${farmDashboard.data.inventory.batchesExpiringWithin30Days} inventory batch(es) expire within 30 days.`,
        path: '/inventory',
        action: 'Review inventory',
      })
    }
  }

  const summaryCards = farmDashboard.data
    ? [
        farmDashboard.data.fields && {
          label: 'Active fields',
          value: farmDashboard.data.fields.active,
          detail: `${farmDashboard.data.fields.totalAreaHectares} ha total`,
        },
        farmDashboard.data.crops && {
          label: 'Planted crops',
          value: farmDashboard.data.crops.planted,
          detail: `${farmDashboard.data.crops.totalAssignments} assignments`,
        },
        farmDashboard.data.workers && {
          label: 'Active workers',
          value: farmDashboard.data.workers.active,
          detail: `${farmDashboard.data.workers.activeApplicators} applicators`,
        },
        farmDashboard.data.inventory && {
          label: 'Stock products',
          value: farmDashboard.data.inventory.productCount,
          detail: `${farmDashboard.data.inventory.batchCount} batches`,
        },
        farmDashboard.data.applications && {
          label: 'Completed applications',
          value: farmDashboard.data.applications.completed,
          detail: `${farmDashboard.data.applications.draft} drafts`,
        },
      ].filter(Boolean) as Array<{ label: string; value: number; detail: string }>
    : []

  return (
    <Stack spacing={4}>
      <Box>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          sx={{
            justifyContent: 'space-between',
            alignItems: { xs: 'flex-start', sm: 'center' },
            gap: 2,
          }}
        >
          <Box>
            <Typography variant="h4" gutterBottom sx={{ fontWeight: 800 }}>
              Hello, {user?.displayName}
            </Typography>
            <Typography color="text.secondary">
              {selectedFarm
                ? `Here is your workspace for ${selectedFarm.name}.`
                : 'Select a farm to begin managing operations.'}
            </Typography>
          </Box>
          {isAdministrator && <Chip label="System administrator" color="primary" />}
        </Stack>
      </Box>

      {isLoading && (
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <CircularProgress size={22} />
          <Typography color="text.secondary">Loading farms…</Typography>
        </Stack>
      )}
      {error && <Alert severity="error">{error.message}</Alert>}
      {dashboardError && (
        <Alert
          severity={
            dashboardError instanceof ApiError && dashboardError.status === 403
              ? 'warning'
              : 'error'
          }
        >
          {dashboardError instanceof ApiError && dashboardError.status === 403
            ? 'You do not have permission to view this dashboard summary.'
            : dashboardError instanceof ApiError
              ? dashboardError.message
              : 'Could not load dashboard statistics.'}
        </Alert>
      )}
      {alerts.length > 0 && (
        <Stack spacing={1.5}>
          {alerts.map((item) => (
            <Alert
              key={item.message}
              severity={item.severity}
              action={
                <Button component={RouterLink} to={item.path} color="inherit" size="small">
                  {item.action}
                </Button>
              }
            >
              {item.message}
            </Alert>
          ))}
        </Stack>
      )}
      {!isLoading && !error && farms.length === 0 && (
        <Alert severity="info">
          No active farms are available for this account. Ask an administrator to create or assign
          one.
        </Alert>
      )}

      {selectedFarm && (
        <Card
          sx={{
            color: 'common.white',
            background: 'linear-gradient(120deg, #1b5e20, #388e3c)',
          }}
        >
          <CardContent sx={{ p: { xs: 3, sm: 4 } }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              sx={{ justifyContent: 'space-between', gap: 2 }}
            >
              <Box>
                <Typography variant="overline" sx={{ opacity: 0.8 }}>
                  Current farm · {selectedFarm.code}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 800 }}>
                  {selectedFarm.name}
                </Typography>
                <Typography sx={{ opacity: 0.85, mt: 0.5 }}>
                  {selectedFarm.location || 'No location recorded'}
                </Typography>
              </Box>
              {hasPermission('fields.view') && (
                <Button
                  component={RouterLink}
                  to="/fields"
                  variant="contained"
                  color="inherit"
                  endIcon={<ArrowForwardRoundedIcon />}
                  sx={{ alignSelf: { xs: 'stretch', sm: 'center' }, color: 'primary.dark' }}
                >
                  Open fields
                </Button>
              )}
            </Stack>
          </CardContent>
        </Card>
      )}

      {(farmDashboard.isLoading || systemDashboard.isLoading) && (
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <CircularProgress size={22} />
          <Typography color="text.secondary">Loading operational summary…</Typography>
        </Stack>
      )}

      {systemDashboard.data && (
        <Card variant="outlined">
          <CardContent>
            <Typography variant="h6" sx={{ fontWeight: 750, mb: 2 }}>
              System overview
            </Typography>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' },
                gap: 2,
              }}
            >
              {[
                ['Active farms', systemDashboard.data.activeFarms],
                ['Active fields', systemDashboard.data.activeFields],
                ['Active workers', systemDashboard.data.activeWorkers],
                ['Completed applications', systemDashboard.data.completedApplications],
              ].map(([label, value]) => (
                <Box key={String(label)}>
                  <Typography variant="h4" sx={{ fontWeight: 800 }}>{value}</Typography>
                  <Typography variant="body2" color="text.secondary">{label}</Typography>
                </Box>
              ))}
            </Box>
          </CardContent>
        </Card>
      )}

      {summaryCards.length > 0 && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(5, 1fr)' },
            gap: 2,
          }}
        >
          {summaryCards.map((summary) => (
            <Card key={summary.label} variant="outlined">
              <CardContent>
                <Typography variant="h4" sx={{ fontWeight: 800 }}>{summary.value}</Typography>
                <Typography sx={{ fontWeight: 700 }}>{summary.label}</Typography>
                <Typography variant="caption" color="text.secondary">{summary.detail}</Typography>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {farmDashboard.data &&
        (farmDashboard.data.recentApplications.length > 0 ||
          farmDashboard.data.recentInventoryTransactions.length > 0) && (
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' }, gap: 2 }}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" sx={{ fontWeight: 750, mb: 2 }}>
                  Recent applications
                </Typography>
                <Stack spacing={1.5}>
                  {farmDashboard.data.recentApplications.map((item) => (
                    <Stack
                      key={item.id}
                      direction="row"
                      sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1 }}
                    >
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {item.fieldName} · {item.cropName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {item.targetPest} · {item.workerName} ·{' '}
                          {new Date(item.applicationDateUtc).toLocaleDateString()}
                        </Typography>
                      </Box>
                      <Button
                        component={RouterLink}
                        to={`/application-history?detail=${item.id}`}
                        size="small"
                      >
                        Details
                      </Button>
                    </Stack>
                  ))}
                </Stack>
              </CardContent>
            </Card>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" sx={{ fontWeight: 750, mb: 2 }}>
                  Recent inventory activity
                </Typography>
                <Stack spacing={1.5}>
                  {farmDashboard.data.recentInventoryTransactions.map((item) => (
                    <Box key={item.id}>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {item.productName} · {item.transactionType}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {item.quantity} · {item.batchNumber ?? 'No batch'} ·{' '}
                        {new Date(item.createdAtUtc).toLocaleDateString()}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          </Box>
        )}

      {visibleShortcuts.length > 0 && (
        <Box>
          <Typography variant="h6" gutterBottom sx={{ fontWeight: 750 }}>
            Farm operations
          </Typography>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
              gap: 2,
            }}
          >
            {visibleShortcuts.map((shortcut) => (
              <Card key={shortcut.path} variant="outlined">
                <CardActionArea component={RouterLink} to={shortcut.path} sx={{ height: '100%' }}>
                  <CardContent sx={{ p: 3 }}>
                    <Box sx={{ color: 'primary.main', mb: 2 }}>{shortcut.icon}</Box>
                    <Typography variant="h6" gutterBottom sx={{ fontWeight: 700 }}>
                      {shortcut.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {shortcut.description}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            ))}
          </Box>
        </Box>
      )}
    </Stack>
  )
}
