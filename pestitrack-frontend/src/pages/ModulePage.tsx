import { Alert, Box, Card, CardContent, Chip, Stack, Typography } from '@mui/material'
import ConstructionRoundedIcon from '@mui/icons-material/ConstructionRounded'
import { useFarm } from '../farms/FarmContext'

interface ModulePageProps {
  title: string
  description: string
  global?: boolean
}

export function ModulePage({ title, description, global = false }: ModulePageProps) {
  const { selectedFarm } = useFarm()

  return (
    <Stack spacing={3}>
      <Box>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 1 }}>
          <Typography variant="h4" sx={{ fontWeight: 800 }}>
            {title}
          </Typography>
          <Chip label="Foundation ready" size="small" variant="outlined" />
        </Stack>
        <Typography color="text.secondary">{description}</Typography>
      </Box>

      {!global && !selectedFarm && (
        <Alert severity="info">Choose a farm before using this module.</Alert>
      )}

      <Card variant="outlined">
        <CardContent sx={{ p: { xs: 3, sm: 5 }, textAlign: 'center' }}>
          <ConstructionRoundedIcon color="primary" sx={{ fontSize: 48, mb: 2 }} />
          <Typography variant="h6" gutterBottom sx={{ fontWeight: 700 }}>
            Module UI is ready to be added
          </Typography>
          <Typography color="text.secondary" sx={{ maxWidth: 560, mx: 'auto' }}>
            Authentication, secure API requests, routing, navigation, and farm selection are already
            connected. The next step is implementing this module&apos;s tables and forms.
          </Typography>
        </CardContent>
      </Card>
    </Stack>
  )
}
