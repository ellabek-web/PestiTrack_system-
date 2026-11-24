import { Link as RouterLink } from 'react-router-dom'
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

const shortcuts = [
  {
    title: 'Fields and crops',
    description: 'Plan crop cycles and manage every field.',
    path: '/fields',
    icon: <GrassRoundedIcon />,
  },
  {
    title: 'Inventory',
    description: 'Review balances, receipts, issues, and adjustments.',
    path: '/inventory',
    icon: <Inventory2RoundedIcon />,
  },
  {
    title: 'Applications',
    description: 'Draft and complete pesticide applications.',
    path: '/applications',
    icon: <AppsRoundedIcon />,
  },
  {
    title: 'Workers',
    description: 'Manage workers and applicator certifications.',
    path: '/workers',
    icon: <PeopleRoundedIcon />,
  },
]

export function DashboardPage() {
  const { user, isAdministrator } = useAuth()
  const { selectedFarm, farms, isLoading, error } = useFarm()

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
            </Stack>
          </CardContent>
        </Card>
      )}

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
          {shortcuts.map((shortcut) => (
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
    </Stack>
  )
}
