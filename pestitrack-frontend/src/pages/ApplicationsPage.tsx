import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import AddRoundedIcon from '@mui/icons-material/AddRounded'
import AppsRoundedIcon from '@mui/icons-material/AppsRounded'
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded'
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded'
import { ApplicationDetailDialog } from '../components/ApplicationDetailDialog'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type {
  ApplicationSafetyCheck,
  Field,
  FieldCrop,
  InventoryBalance,
  PesticideApplication,
  Product,
  WeatherRecord,
  Worker,
} from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog form resets from the selected application */

interface ProductLinePayload {
  productId: string
  batchNumber: string
  quantityUsed: number
  applicationRate: number
  rateUnit: string
}

interface ApplicationPayload {
  fieldId: string
  fieldCropId: string
  workerId: string
  applicationDateUtc: string
  targetPest: string
  applicationMethod: string
  equipment: string | null
  areaTreatedHectares: number
  weatherRecordId: string | null
  notes: string | null
  products: ProductLinePayload[]
}

interface EditableProductLine {
  key: string
  productId: string
  batchNumber: string
  quantityUsed: string
  applicationRate: string
  rateUnit: string
}

function isPlanted(status: FieldCrop['status']) {
  return status === 'Planted' || status === 1
}

function toLocalInput(value: string): string {
  const date = new Date(value)
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function newProductLine(balances: InventoryBalance[]): EditableProductLine {
  const first = balances[0]
  return {
    key: crypto.randomUUID(),
    productId: first?.productId ?? '',
    batchNumber: first?.batchNumber ?? '',
    quantityUsed: '',
    applicationRate: '',
    rateUnit: 'L/ha',
  }
}

interface ApplicationFormProps {
  open: boolean
  application: PesticideApplication | null
  farmId: string
  fields: Field[]
  workers: Worker[]
  products: Product[]
  balances: InventoryBalance[]
  weather: WeatherRecord[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: ApplicationPayload) => void
}

function ApplicationForm({
  open,
  application,
  farmId,
  fields,
  workers,
  products,
  balances,
  weather,
  saving,
  error,
  onClose,
  onSave,
}: ApplicationFormProps) {
  const [fieldId, setFieldId] = useState('')
  const [fieldCropId, setFieldCropId] = useState('')
  const [workerId, setWorkerId] = useState('')
  const [applicationDate, setApplicationDate] = useState('')
  const [targetPest, setTargetPest] = useState('')
  const [applicationMethod, setApplicationMethod] = useState('')
  const [equipment, setEquipment] = useState('')
  const [area, setArea] = useState('')
  const [weatherRecordId, setWeatherRecordId] = useState('')
  const [notes, setNotes] = useState('')
  const [productLines, setProductLines] = useState<EditableProductLine[]>([])

  const activeFields = useMemo(
    () => fields.filter((field) => field.isActive),
    [fields],
  )
  const firstActiveFieldId = activeFields[0]?.id ?? ''
  const effectiveFieldId = activeFields.some((field) => field.id === fieldId)
    ? fieldId
    : firstActiveFieldId

  const fieldCropsQuery = useQuery({
    queryKey: ['field-crops', farmId, effectiveFieldId],
    queryFn: () =>
      apiRequest<FieldCrop[]>(`/api/farms/${farmId}/fields/${effectiveFieldId}/crops`),
    enabled: open && Boolean(effectiveFieldId),
    retry: false,
  })
  const plantedCrops = useMemo(
    () => (fieldCropsQuery.data ?? []).filter((crop) => isPlanted(crop.status)),
    [fieldCropsQuery.data],
  )

  useEffect(() => {
    if (!open) return
    setFieldId(application?.fieldId ?? firstActiveFieldId)
    setFieldCropId(application?.fieldCropId ?? '')
    setWorkerId(application?.workerId ?? workers[0]?.id ?? '')
    setApplicationDate(
      toLocalInput(application?.applicationDateUtc ?? new Date().toISOString()),
    )
    setTargetPest(application?.targetPest ?? '')
    setApplicationMethod(application?.applicationMethod ?? '')
    setEquipment(application?.equipment ?? '')
    setArea(application?.areaTreatedHectares?.toString() ?? '')
    setWeatherRecordId(application?.weatherRecordId ?? '')
    setNotes(application?.notes ?? '')
    setProductLines(
      application
        ? application.products.map((line) => ({
            key: line.id,
            productId: line.productId,
            batchNumber: line.batchNumber,
            quantityUsed: line.quantityUsed.toString(),
            applicationRate: line.applicationRate.toString(),
            rateUnit: line.rateUnit,
          }))
        : [newProductLine(balances)],
    )
  }, [open, application, firstActiveFieldId, workers, balances])

  useEffect(() => {
    if (!open || application || !plantedCrops.length || fieldCropId) return
    setFieldCropId(plantedCrops[0].id)
  }, [open, application, plantedCrops, fieldCropId])

  const updateLine = (key: string, patch: Partial<EditableProductLine>) => {
    setProductLines((current) =>
      current.map((line) => (line.key === key ? { ...line, ...patch } : line)),
    )
  }

  const changeLineProduct = (line: EditableProductLine, productId: string) => {
    const firstBatch = balances.find((balance) => balance.productId === productId)
    updateLine(line.key, {
      productId,
      batchNumber: firstBatch?.batchNumber ?? '',
    })
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      fieldId: effectiveFieldId,
      fieldCropId,
      workerId,
      applicationDateUtc: new Date(applicationDate).toISOString(),
      targetPest: targetPest.trim(),
      applicationMethod: applicationMethod.trim(),
      equipment: equipment.trim() || null,
      areaTreatedHectares: Number(area),
      weatherRecordId: weatherRecordId || null,
      notes: notes.trim() || null,
      products: productLines.map((line) => ({
        productId: line.productId,
        batchNumber: line.batchNumber,
        quantityUsed: Number(line.quantityUsed),
        applicationRate: Number(line.applicationRate),
        rateUnit: line.rateUnit.trim(),
      })),
    })
  }

  const weatherOptions = weather.filter(
    (record) => record.fieldId == null || record.fieldId === effectiveFieldId,
  )
  const applicators = workers.filter((worker) => worker.isActive && worker.isApplicator)
  const productsWithStock = products.filter(
    (product) =>
      product.isActive && balances.some((balance) => balance.productId === product.id),
  )

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="lg">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{application ? 'Edit draft application' : 'New pesticide application'}</DialogTitle>
        <DialogContent>
          <Stack spacing={3} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            {!productsWithStock.length && (
              <Alert severity="warning">
                Receive product inventory before creating an application.
              </Alert>
            )}
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
                gap: 2.5,
              }}
            >
              <FormControl required>
                <InputLabel id="application-field-label">Field</InputLabel>
                <Select
                  labelId="application-field-label"
                  label="Field"
                  value={effectiveFieldId}
                  onChange={(event) => {
                    setFieldId(event.target.value)
                    setFieldCropId('')
                    setWeatherRecordId('')
                  }}
                >
                  {activeFields.map((field) => (
                    <MenuItem key={field.id} value={field.id}>
                      {field.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl required>
                <InputLabel id="application-crop-label">Planted crop</InputLabel>
                <Select
                  labelId="application-crop-label"
                  label="Planted crop"
                  value={fieldCropId}
                  onChange={(event) => setFieldCropId(event.target.value)}
                >
                  {plantedCrops.map((crop) => (
                    <MenuItem key={crop.id} value={crop.id}>
                      {crop.cropName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl required>
                <InputLabel id="applicator-label">Applicator</InputLabel>
                <Select
                  labelId="applicator-label"
                  label="Applicator"
                  value={workerId}
                  onChange={(event) => setWorkerId(event.target.value)}
                >
                  {applicators.map((worker) => (
                    <MenuItem key={worker.id} value={worker.id}>
                      {worker.fullName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="Application date"
                type="datetime-local"
                value={applicationDate}
                onChange={(event) => setApplicationDate(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                required
              />
              <TextField
                label="Target pest"
                value={targetPest}
                onChange={(event) => setTargetPest(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 200 } }}
                required
              />
              <TextField
                label="Application method"
                value={applicationMethod}
                onChange={(event) => setApplicationMethod(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
                placeholder="Broadcast spray, spot treatment…"
                required
              />
              <TextField
                label="Equipment"
                value={equipment}
                onChange={(event) => setEquipment(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 200 } }}
              />
              <TextField
                label="Area treated"
                type="number"
                value={area}
                onChange={(event) => setArea(event.target.value)}
                slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                helperText="Hectares"
                required
              />
              <FormControl>
                <InputLabel id="application-weather-label">Weather record</InputLabel>
                <Select
                  labelId="application-weather-label"
                  label="Weather record"
                  value={weatherRecordId}
                  onChange={(event) => setWeatherRecordId(event.target.value)}
                >
                  <MenuItem value="">None</MenuItem>
                  {weatherOptions.map((record) => (
                    <MenuItem key={record.id} value={record.id}>
                      {new Date(record.recordedAtUtc).toLocaleString()} ·{' '}
                      {record.conditions ?? 'No conditions'}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>

            <Box>
              <Stack
                direction="row"
                sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1.5 }}
              >
                <Typography variant="h6" sx={{ fontWeight: 750 }}>
                  Products used
                </Typography>
                <Button
                  startIcon={<AddRoundedIcon />}
                  onClick={() =>
                    setProductLines((current) => [...current, newProductLine(balances)])
                  }
                  disabled={!productsWithStock.length}
                >
                  Add product
                </Button>
              </Stack>
              <Stack spacing={2}>
                {productLines.map((line, index) => {
                  const batches = balances.filter(
                    (balance) => balance.productId === line.productId,
                  )
                  const available = batches.find(
                    (balance) => balance.batchNumber === line.batchNumber,
                  )?.quantityOnHand
                  return (
                    <Card key={line.key} variant="outlined" sx={{ p: 2 }}>
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: '1fr',
                            md: '2fr 1.5fr 1fr 1fr 1fr auto',
                          },
                          gap: 2,
                          alignItems: 'start',
                        }}
                      >
                        <FormControl required size="small">
                          <InputLabel id={`product-${line.key}`}>Product</InputLabel>
                          <Select
                            labelId={`product-${line.key}`}
                            label="Product"
                            value={line.productId}
                            onChange={(event) =>
                              changeLineProduct(line, event.target.value)
                            }
                          >
                            {productsWithStock.map((product) => (
                              <MenuItem key={product.id} value={product.id}>
                                {product.name}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                        <FormControl required size="small">
                          <InputLabel id={`batch-${line.key}`}>Batch</InputLabel>
                          <Select
                            labelId={`batch-${line.key}`}
                            label="Batch"
                            value={line.batchNumber}
                            onChange={(event) =>
                              updateLine(line.key, { batchNumber: event.target.value })
                            }
                          >
                            {batches.map((batch) => (
                              <MenuItem key={batch.batchNumber} value={batch.batchNumber}>
                                {batch.batchNumber} ({batch.quantityOnHand})
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                        <TextField
                          label="Quantity"
                          type="number"
                          size="small"
                          value={line.quantityUsed}
                          onChange={(event) =>
                            updateLine(line.key, { quantityUsed: event.target.value })
                          }
                          slotProps={{
                            htmlInput: { min: 0.001, max: available, step: 0.001 },
                          }}
                          helperText={available == null ? '' : `${available} available`}
                          required
                        />
                        <TextField
                          label="Rate"
                          type="number"
                          size="small"
                          value={line.applicationRate}
                          onChange={(event) =>
                            updateLine(line.key, { applicationRate: event.target.value })
                          }
                          slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                          required
                        />
                        <TextField
                          label="Rate unit"
                          size="small"
                          value={line.rateUnit}
                          onChange={(event) =>
                            updateLine(line.key, { rateUnit: event.target.value })
                          }
                          slotProps={{ htmlInput: { maxLength: 50 } }}
                          required
                        />
                        <Tooltip title="Remove product">
                          <span>
                            <IconButton
                              onClick={() =>
                                setProductLines((current) =>
                                  current.filter((item) => item.key !== line.key),
                                )
                              }
                              disabled={productLines.length === 1}
                              aria-label={`Remove product ${index + 1}`}
                            >
                              <DeleteOutlineRoundedIcon />
                            </IconButton>
                          </span>
                        </Tooltip>
                      </Box>
                    </Card>
                  )
                })}
              </Stack>
            </Box>

            <TextField
              label="Notes"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 1000 } }}
              multiline
              minRows={3}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={
              saving ||
              !plantedCrops.length ||
              !applicators.length ||
              !productsWithStock.length
            }
          >
            {saving ? 'Saving…' : application ? 'Save draft' : 'Create draft'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

interface CompleteDialogProps {
  farmId: string | null
  application: PesticideApplication | null
  balances: InventoryBalance[]
  completing: boolean
  error: string
  onClose: () => void
  onComplete: () => void
}

function CompleteDialog({
  farmId,
  application,
  balances,
  completing,
  error,
  onClose,
  onComplete,
}: CompleteDialogProps) {
  const safetyQuery = useQuery({
    queryKey: ['application-safety-check', farmId, application?.id],
    queryFn: () =>
      apiRequest<ApplicationSafetyCheck>(
        `/api/farms/${farmId}/applications/${application?.id}/safety-check`,
      ),
    enabled: Boolean(farmId && application?.id),
    retry: false,
  })

  const checks =
    application?.products.map((line) => {
      const available =
        balances.find(
          (balance) =>
            balance.productId === line.productId &&
            balance.batchNumber.toLowerCase() === line.batchNumber.toLowerCase(),
        )?.quantityOnHand ?? 0
      return { line, available, sufficient: available >= line.quantityUsed }
    }) ?? []
  const hasSafetyViolations = Boolean(safetyQuery.data && !safetyQuery.data.isCompliant)
  const canComplete =
    checks.length > 0 &&
    checks.every((check) => check.sufficient) &&
    !hasSafetyViolations

  return (
    <Dialog open={Boolean(application)} onClose={completing ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>Complete pesticide application?</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <Alert severity="warning">
            Completion permanently issues inventory and the application can no longer be edited.
          </Alert>
          {safetyQuery.data && (
            <Stack spacing={1}>
              {!safetyQuery.data.isCompliant && (
                <Alert severity="error">
                  Safety check found {safetyQuery.data.violationCount} violation(s). Resolve them before completing.
                </Alert>
              )}
              {safetyQuery.data.checks
                .filter((item) => item.severity !== 'Pass')
                .map((item, idx) => (
                  <Alert
                    key={`${item.code}-${idx}`}
                    severity={item.severity === 'Violation' ? 'error' : 'warning'}
                    sx={{ py: 0.5 }}
                  >
                    <strong>{item.title}:</strong> {item.message}
                  </Alert>
                ))}
              {(safetyQuery.data.reEntryAllowedAtUtc || safetyQuery.data.earliestSafeHarvestDate) && (
                <Alert severity="info" sx={{ py: 0.5 }}>
                  {safetyQuery.data.reEntryAllowedAtUtc && (
                    <span>
                      REI until {new Date(safetyQuery.data.reEntryAllowedAtUtc).toLocaleString()}
                    </span>
                  )}
                  {safetyQuery.data.reEntryAllowedAtUtc && safetyQuery.data.earliestSafeHarvestDate && ' · '}
                  {safetyQuery.data.earliestSafeHarvestDate && (
                    <span>Earliest safe harvest: {safetyQuery.data.earliestSafeHarvestDate}</span>
                  )}
                </Alert>
              )}
            </Stack>
          )}
          {checks.map(({ line, available, sufficient }) => (
            <Stack
              key={line.id}
              direction="row"
              sx={{ justifyContent: 'space-between', alignItems: 'center' }}
            >
              <Box>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>
                  {line.productName} · {line.batchNumber}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Requires {line.quantityUsed}; {available} available
                </Typography>
              </Box>
              <Chip
                size="small"
                label={sufficient ? 'Available' : 'Insufficient'}
                color={sufficient ? 'success' : 'error'}
              />
            </Stack>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ p: 3 }}>
        <Button onClick={onClose} disabled={completing}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="success"
          onClick={onComplete}
          disabled={completing || !canComplete}
        >
          {completing ? 'Completing…' : 'Complete and issue stock'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

export function ApplicationsPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canCreate = hasPermission('applications.create')
  const canComplete = hasPermission('applications.approve') && hasPermission('inventory.issue')
  const [formOpen, setFormOpen] = useState(false)
  const [editingApplication, setEditingApplication] =
    useState<PesticideApplication | null>(null)
  const [formError, setFormError] = useState('')
  const [completingApplication, setCompletingApplication] =
    useState<PesticideApplication | null>(null)
  const [completionError, setCompletionError] = useState('')
  const [viewingApplicationId, setViewingApplicationId] = useState<string | null>(null)
  const queryKey = ['applications', selectedFarmId]

  const applicationsQuery = useQuery({
    queryKey,
    queryFn: () =>
      apiRequest<PesticideApplication[]>(`/api/farms/${selectedFarmId}/applications`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId) && canCreate && hasPermission('fields.view'),
    retry: false,
  })
  const workersQuery = useQuery({
    queryKey: ['workers', selectedFarmId],
    queryFn: () => apiRequest<Worker[]>(`/api/farms/${selectedFarmId}/workers`),
    enabled: Boolean(selectedFarmId) && canCreate && hasPermission('workers.view'),
    retry: false,
  })
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => apiRequest<Product[]>('/api/products'),
    enabled: canCreate,
    retry: false,
  })
  const balancesQuery = useQuery({
    queryKey: ['inventory-balances', selectedFarmId],
    queryFn: () =>
      apiRequest<InventoryBalance[]>(`/api/farms/${selectedFarmId}/inventory/balances`),
    enabled: Boolean(selectedFarmId) && (canCreate || canComplete) && hasPermission('inventory.view'),
    retry: false,
  })
  const weatherQuery = useQuery({
    queryKey: ['weather', selectedFarmId, '', '', ''],
    queryFn: () =>
      apiRequest<WeatherRecord[]>(`/api/farms/${selectedFarmId}/weather-records`),
    enabled: Boolean(selectedFarmId) && canCreate && hasPermission('weather.view'),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: ApplicationPayload) =>
      apiRequest<PesticideApplication>(
        editingApplication
          ? `/api/farms/${selectedFarmId}/applications/${editingApplication.id}`
          : `/api/farms/${selectedFarmId}/applications`,
        {
          method: editingApplication ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey })
      setFormOpen(false)
      setEditingApplication(null)
    },
    onError: (caught) =>
      setFormError(
        caught instanceof ApiError ? caught.message : 'Could not save the application.',
      ),
  })

  const completeMutation = useMutation({
    mutationFn: (application: PesticideApplication) =>
      apiRequest<PesticideApplication>(
        `/api/farms/${selectedFarmId}/applications/${application.id}/complete`,
        { method: 'POST' },
      ),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey }),
        queryClient.invalidateQueries({ queryKey: ['inventory-balances', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['inventory-transactions', selectedFarmId] }),
      ])
      setCompletingApplication(null)
    },
    onError: (caught) =>
      setCompletionError(
        caught instanceof ApiError ? caught.message : 'Could not complete the application.',
      ),
  })

  const openForm = (application: PesticideApplication | null) => {
    setEditingApplication(application)
    setFormError('')
    setFormOpen(true)
  }

  const openCompletion = (application: PesticideApplication) => {
    setCompletingApplication(application)
    setCompletionError('')
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to manage pesticide applications.</Alert>
  }

  const pageError =
    applicationsQuery.error ??
    fieldsQuery.error ??
    workersQuery.error ??
    productsQuery.error ??
    balancesQuery.error ??
    weatherQuery.error

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Pesticide applications
          </Typography>
          <Typography color="text.secondary">
            Plan, review, and complete treatments for {selectedFarm?.name}.
          </Typography>
        </Box>
        {canCreate && (
          <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => openForm(null)}>
            New application
          </Button>
        )}
      </Stack>

      {pageError && (
        <Alert severity="error">
          {pageError instanceof ApiError ? pageError.message : 'Could not load application data.'}
        </Alert>
      )}

      <Card variant="outlined">
        {applicationsQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !applicationsQuery.data?.length ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <AppsRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No pesticide applications
            </Typography>
            <Typography color="text.secondary">
              Create a draft application for a planted crop.
            </Typography>
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Date</TableCell>
                  <TableCell>Field / crop</TableCell>
                  <TableCell>Target</TableCell>
                  <TableCell>Applicator</TableCell>
                  <TableCell>Products</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {applicationsQuery.data.map((application) => (
                  <TableRow key={application.id} hover>
                    <TableCell>{new Date(application.applicationDateUtc).toLocaleString()}</TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {application.fieldName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {application.cropName} · {application.areaTreatedHectares} ha
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{application.targetPest}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {application.applicationMethod}
                      </Typography>
                    </TableCell>
                    <TableCell>{application.workerName}</TableCell>
                    <TableCell>
                      <Stack spacing={0.5}>
                        {application.products.map((line) => (
                          <Typography key={line.id} variant="caption">
                            {line.productName}: {line.quantityUsed} ({line.batchNumber})
                          </Typography>
                        ))}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={application.status}
                        size="small"
                        color={application.status === 'Completed' ? 'success' : 'warning'}
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View details">
                        <IconButton
                          onClick={() => setViewingApplicationId(application.id)}
                          aria-label="View application details"
                        >
                          <VisibilityRoundedIcon />
                        </IconButton>
                      </Tooltip>
                      {application.status === 'Draft' && (
                        <>
                          {canCreate && (
                            <Tooltip title="Edit draft">
                              <IconButton
                                onClick={() => openForm(application)}
                                aria-label="Edit draft application"
                              >
                                <EditRoundedIcon />
                              </IconButton>
                            </Tooltip>
                          )}
                          {canComplete && (
                            <Tooltip title="Complete application">
                              <IconButton
                                color="success"
                                onClick={() => openCompletion(application)}
                                aria-label="Complete application"
                              >
                                <CheckCircleRoundedIcon />
                              </IconButton>
                            </Tooltip>
                          )}
                        </>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Card>

      <ApplicationForm
        open={formOpen}
        application={editingApplication}
        farmId={selectedFarmId}
        fields={fieldsQuery.data ?? []}
        workers={workersQuery.data ?? []}
        products={productsQuery.data ?? []}
        balances={balancesQuery.data ?? []}
        weather={weatherQuery.data ?? []}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setFormOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />
      <CompleteDialog
        farmId={selectedFarmId}
        application={completingApplication}
        balances={balancesQuery.data ?? []}
        completing={completeMutation.isPending}
        error={completionError}
        onClose={() => setCompletingApplication(null)}
        onComplete={() => {
          if (completingApplication) completeMutation.mutate(completingApplication)
        }}
      />
      <ApplicationDetailDialog
        farmId={selectedFarmId}
        applicationId={viewingApplicationId}
        onClose={() => setViewingApplicationId(null)}
      />
    </Stack>
  )
}
