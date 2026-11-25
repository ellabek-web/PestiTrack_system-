import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material'
import { ApiError, apiRequest } from '../lib/api'
import type { ApplicationDetail, ApplicationSafetyCheck } from '../types/api'

function displayValue(value: unknown) {
  if (value == null || value === '') return '—'
  return String(value)
}

interface ApplicationDetailDialogProps {
  farmId: string | null
  applicationId: string | null
  onClose: () => void
}

export function ApplicationDetailDialog({
  farmId,
  applicationId,
  onClose,
}: ApplicationDetailDialogProps) {
  const detailQuery = useQuery({
    queryKey: ['application-detail', farmId, applicationId],
    queryFn: () =>
      apiRequest<ApplicationDetail>(
        `/api/farms/${farmId}/applications/${applicationId}`,
      ),
    enabled: Boolean(farmId && applicationId),
    retry: false,
  })

  const safetyQuery = useQuery({
    queryKey: ['application-safety-check', farmId, applicationId],
    queryFn: () =>
      apiRequest<ApplicationSafetyCheck>(
        `/api/farms/${farmId}/applications/${applicationId}/safety-check`,
      ),
    enabled: Boolean(farmId && applicationId),
    retry: false,
  })

  return (
    <Dialog open={Boolean(applicationId)} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>Application details</DialogTitle>
      <DialogContent>
        {detailQuery.isLoading ? (
          <Box sx={{ p: 5, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : detailQuery.error ? (
          <Alert severity="error">
            {detailQuery.error instanceof ApiError
              ? detailQuery.error.message
              : 'Could not load application details.'}
          </Alert>
        ) : detailQuery.data ? (
          <Stack spacing={3} sx={{ pt: 1 }}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2,
              }}
            >
              {[
                ['Date', new Date(detailQuery.data.applicationDateUtc).toLocaleString()],
                ['Status', detailQuery.data.status],
                ['Field', detailQuery.data.fieldName],
                ['Crop', detailQuery.data.cropName],
                ['Applicator', detailQuery.data.workerName],
                ['Target pest', detailQuery.data.targetPest],
                ['Method', detailQuery.data.applicationMethod],
                ['Equipment', displayValue(detailQuery.data.equipment)],
                ['Area treated', `${detailQuery.data.areaTreatedHectares} ha`],
                [
                  'Created by',
                  detailQuery.data.createdByEmail
                    ? `${detailQuery.data.createdByDisplayName} (${detailQuery.data.createdByEmail})`
                    : detailQuery.data.createdByDisplayName,
                ],
              ].map(([label, value]) => (
                <Box key={label}>
                  <Typography variant="caption" color="text.secondary">
                    {label}
                  </Typography>
                  <Typography>{value}</Typography>
                </Box>
              ))}
            </Box>

            {detailQuery.data.notes && (
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Notes
                </Typography>
                <Typography variant="body2">{detailQuery.data.notes}</Typography>
              </Box>
            )}

            {detailQuery.data.weather && (
              <Alert severity="info">
                Weather ({new Date(detailQuery.data.weather.recordedAtUtc).toLocaleString()}):{' '}
                {displayValue(detailQuery.data.weather.conditions)} ·{' '}
                {displayValue(detailQuery.data.weather.temperatureCelsius)} °C ·{' '}
                {displayValue(detailQuery.data.weather.humidityPercentage)}% humidity · Wind{' '}
                {displayValue(detailQuery.data.weather.windSpeedMetersPerSecond)} m/s
              </Alert>
            )}

            {safetyQuery.data && (
              <Box>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 1, gap: 1 }}
                >
                  <Typography variant="h6" sx={{ fontWeight: 750 }}>
                    Safety and compliance check
                  </Typography>
                  <Stack direction="row" spacing={1}>
                    <Chip
                      size="small"
                      label={safetyQuery.data.isCompliant ? 'Compliant' : `${safetyQuery.data.violationCount} violation(s)`}
                      color={safetyQuery.data.isCompliant ? 'success' : 'error'}
                    />
                    {safetyQuery.data.warningCount > 0 && (
                      <Chip
                        size="small"
                        label={`${safetyQuery.data.warningCount} warning(s)`}
                        color="warning"
                      />
                    )}
                  </Stack>
                </Stack>
                {(safetyQuery.data.reEntryAllowedAtUtc || safetyQuery.data.earliestSafeHarvestDate) && (
                  <Alert severity="info" sx={{ mb: 1.5 }}>
                    {safetyQuery.data.reEntryAllowedAtUtc && (
                      <span>
                        Re-entry allowed after:{' '}
                        <strong>{new Date(safetyQuery.data.reEntryAllowedAtUtc).toLocaleString()}</strong>
                      </span>
                    )}
                    {safetyQuery.data.reEntryAllowedAtUtc && safetyQuery.data.earliestSafeHarvestDate && ' · '}
                    {safetyQuery.data.earliestSafeHarvestDate && (
                      <span>
                        Earliest safe harvest: <strong>{safetyQuery.data.earliestSafeHarvestDate}</strong>
                      </span>
                    )}
                  </Alert>
                )}
                <Stack spacing={1}>
                  {safetyQuery.data.checks.map((check, index) => (
                    <Alert
                      key={`${check.code}-${index}`}
                      severity={
                        check.severity === 'Violation'
                          ? 'error'
                          : check.severity === 'Warning'
                            ? 'warning'
                            : 'success'
                      }
                      sx={{ py: 0.5 }}
                    >
                      <strong>{check.title}:</strong> {check.message}
                    </Alert>
                  ))}
                </Stack>
              </Box>
            )}

            <Box>
              <Typography variant="h6" sx={{ fontWeight: 750, mb: 1 }}>
                Products and inventory traceability
              </Typography>
              <Stack spacing={1.5}>
                {detailQuery.data.products.map((product) => (
                  <Card key={product.id} variant="outlined" sx={{ p: 2 }}>
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 1 }}
                    >
                      <Box>
                        <Typography sx={{ fontWeight: 700 }}>
                          {product.productName} · Batch {product.batchNumber}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {product.quantityUsed} used at {product.applicationRate}{' '}
                          {product.rateUnit}
                        </Typography>
                      </Box>
                      <Chip
                        size="small"
                        label={product.inventoryTransaction ? 'Stock deducted' : 'Not deducted yet'}
                        color={product.inventoryTransaction ? 'success' : 'default'}
                      />
                    </Stack>
                    {product.inventoryTransaction && (
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', mt: 1, fontFamily: 'monospace' }}
                      >
                        Tx {product.inventoryTransaction.id} ·{' '}
                        {product.inventoryTransaction.transactionType} ·{' '}
                        {new Date(product.inventoryTransaction.createdAtUtc).toLocaleString()}
                      </Typography>
                    )}
                  </Card>
                ))}
              </Stack>
            </Box>
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  )
}
