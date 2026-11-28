import { useState, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  Divider,
  Drawer,
  FormControl,
  IconButton,
  InputLabel,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import AgricultureRoundedIcon from '@mui/icons-material/AgricultureRounded'
import AppsRoundedIcon from '@mui/icons-material/AppsRounded'
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded'
import GrassRoundedIcon from '@mui/icons-material/GrassRounded'
import GridViewRoundedIcon from '@mui/icons-material/GridViewRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded'
import MenuRoundedIcon from '@mui/icons-material/MenuRounded'
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded'
import ScienceRoundedIcon from '@mui/icons-material/ScienceRounded'
import SpaRoundedIcon from '@mui/icons-material/SpaRounded'
import AdminPanelSettingsRoundedIcon from '@mui/icons-material/AdminPanelSettingsRounded'
import AssessmentRoundedIcon from '@mui/icons-material/AssessmentRounded'
import NotificationsRoundedIcon from '@mui/icons-material/NotificationsRounded'
import FactCheckRoundedIcon from '@mui/icons-material/FactCheckRounded'
import HealthAndSafetyRoundedIcon from '@mui/icons-material/HealthAndSafetyRounded'
import HistoryRoundedIcon from '@mui/icons-material/HistoryRounded'
import ManageAccountsRoundedIcon from '@mui/icons-material/ManageAccountsRounded'
import PersonRoundedIcon from '@mui/icons-material/PersonRounded'
import DomainRoundedIcon from '@mui/icons-material/DomainRounded'
import { useAuth } from '../auth/AuthContext'
import { useFarm } from '../farms/FarmContext'
import { apiRequest } from '../lib/api'
import type { NotificationSummary } from '../types/api'

const drawerWidth = 264

interface NavItem {
  label: string
  path: string
  icon: ReactNode
  permission?: string
  allowSystemAuditor?: boolean
}

const navigation: NavItem[] = [
  { label: 'Overview', path: '/', icon: <DashboardRoundedIcon /> },
  { label: 'Fields', path: '/fields', icon: <GrassRoundedIcon />, permission: 'fields.view' },
  { label: 'Plots', path: '/plots', icon: <GridViewRoundedIcon />, permission: 'plots.view' },
  { label: 'Crops', path: '/crops', icon: <SpaRoundedIcon />, permission: 'crops.view' },
  { label: 'Products', path: '/products', icon: <ScienceRoundedIcon /> },
  { label: 'Workers', path: '/workers', icon: <PeopleRoundedIcon />, permission: 'workers.view' },
  { label: 'Inventory', path: '/inventory', icon: <Inventory2RoundedIcon />, permission: 'inventory.view' },
  { label: 'Applications', path: '/applications', icon: <AppsRoundedIcon />, permission: 'applications.view' },
  { label: 'Application history', path: '/application-history', icon: <HistoryRoundedIcon />, permission: 'applications.view' },
  { label: 'Reports', path: '/reports', icon: <AssessmentRoundedIcon />, permission: 'reports.view' },
  { label: 'Safety & compliance', path: '/compliance', icon: <HealthAndSafetyRoundedIcon />, permission: 'compliance.view' },
  { label: 'Notifications', path: '/notifications', icon: <NotificationsRoundedIcon /> },
  {
    label: 'Audit trail',
    path: '/audit',
    icon: <FactCheckRoundedIcon />,
    permission: 'audit.view',
    allowSystemAuditor: true,
  },
  { label: 'My account', path: '/account', icon: <PersonRoundedIcon /> },
]

export function AppShell() {
  const { user, logout, isAdministrator, isAuditor } = useAuth()
  const { farms, selectedFarmId, setSelectedFarmId, hasPermission } = useFarm()
  const location = useLocation()
  const [mobileOpen, setMobileOpen] = useState(false)

  const notificationsSummaryQuery = useQuery({
    queryKey: ['notifications-summary', selectedFarmId],
    queryFn: () =>
      apiRequest<NotificationSummary>(
        `/api/farms/${selectedFarmId}/notifications/unread-count`,
      ),
    enabled: Boolean(selectedFarmId),
    staleTime: 60_000,
    retry: false,
  })
  const unreadCount = notificationsSummaryQuery.data?.unreadCount ?? 0
  const criticalCount = notificationsSummaryQuery.data?.criticalCount ?? 0

  const visibleNavigation = navigation.filter((item) => {
    if (!item.permission) return true
    if (isAdministrator) return true
    if (item.allowSystemAuditor && isAuditor) return true
    return hasPermission(item.permission)
  })

  const drawer = (
    <Stack sx={{ height: '100%' }}>
      <Toolbar sx={{ px: 2.5, gap: 1.5 }}>
        <AgricultureRoundedIcon color="primary" fontSize="large" />
        <Typography variant="h6" sx={{ fontWeight: 800 }}>
          PestiTrack
        </Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ p: 2 }}>
        <FormControl fullWidth size="small">
          <InputLabel id="farm-label">Farm</InputLabel>
          <Select
            labelId="farm-label"
            label="Farm"
            value={selectedFarmId ?? ''}
            onChange={(event) => setSelectedFarmId(event.target.value)}
            disabled={!farms.length}
          >
            {farms.map((farm) => (
              <MenuItem key={farm.id} value={farm.id}>
                {farm.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>
      <List sx={{ px: 1.5, py: 0, flex: 1 }}>
        {[
          ...visibleNavigation,
          ...(isAdministrator
            ? [
                {
                  label: 'Farms',
                  path: '/farms',
                  icon: <DomainRoundedIcon />,
                },
                {
                  label: 'Farm access',
                  path: '/administration',
                  icon: <AdminPanelSettingsRoundedIcon />,
                },
                {
                  label: 'User accounts',
                  path: '/users',
                  icon: <ManageAccountsRoundedIcon />,
                },
              ]
            : []),
        ].map((item) => {
          const selected =
            item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path)
          return (
            <ListItemButton
              key={item.path}
              component={NavLink}
              to={item.path}
              selected={selected}
              onClick={() => setMobileOpen(false)}
              sx={{ borderRadius: 2, mb: 0.5 }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>
                {item.path === '/notifications' ? (
                  <Badge
                    badgeContent={unreadCount}
                    color={criticalCount > 0 ? 'error' : 'warning'}
                    max={99}
                  >
                    {item.icon}
                  </Badge>
                ) : (
                  item.icon
                )}
              </ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          )
        })}
      </List>
      <Divider />
      <Stack direction="row" spacing={1.5} sx={{ p: 2, alignItems: 'center' }}>
        <Avatar sx={{ width: 38, height: 38, bgcolor: 'primary.main' }}>
          {user?.displayName?.charAt(0).toUpperCase() || 'U'}
        </Avatar>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography variant="body2" noWrap sx={{ fontWeight: 700 }}>
            {user?.displayName}
          </Typography>
          <Typography
            variant="caption"
            color="text.secondary"
            noWrap
            sx={{ display: 'block' }}
          >
            {user?.email}
          </Typography>
        </Box>
        <Tooltip title="Sign out">
          <IconButton aria-label="Sign out" onClick={() => void logout()}>
            <LogoutRoundedIcon />
          </IconButton>
        </Tooltip>
      </Stack>
    </Stack>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          display: { md: 'none' },
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Toolbar>
          <IconButton
            edge="start"
            aria-label="Open navigation"
            onClick={() => setMobileOpen(true)}
          >
            <MenuRoundedIcon />
          </IconButton>
          <Typography variant="h6" sx={{ ml: 1, fontWeight: 800 }}>
            PestiTrack
          </Typography>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: drawerWidth },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' },
          }}
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flex: 1,
          minWidth: 0,
          bgcolor: 'background.default',
          pt: { xs: 10, md: 0 },
        }}
      >
        <Box sx={{ p: { xs: 2, sm: 3, lg: 4 }, maxWidth: 1440, mx: 'auto' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}
