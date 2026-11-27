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
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import AddRoundedIcon from '@mui/icons-material/AddRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import SpaRoundedIcon from '@mui/icons-material/SpaRounded'
import ToggleOffRoundedIcon from '@mui/icons-material/ToggleOffRounded'
import { useAuth } from '../auth/AuthContext'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Crop, Field, FieldCrop, FieldCropStatus } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from the selected record */

const statuses: FieldCropStatus[] = ['Planned', 'Planted', 'Harvested', 'Cancelled']
const numericStatuses: FieldCropStatus[] = ['Planned', 'Planted', 'Harvested', 'Cancelled']

function statusName(status: FieldCrop['status']): FieldCropStatus {
  return typeof status === 'number' ? (numericStatuses[status - 1] ?? 'Planned') : status
}

function statusColor(status: FieldCropStatus): 'default' | 'info' | 'success' | 'error' {
  if (status === 'Planted') return 'info'
  if (status === 'Harvested') return 'success'
  if (status === 'Cancelled') return 'error'
  return 'default'
}

interface PlantingPayload {
  cropId: string
  plantingDate: string
  expectedHarvestDate: string | null
  actualHarvestDate?: string | null
  areaHectares: number | null
}

interface PlantingFormProps {
  open: boolean
  planting: FieldCrop | null
  crops: Crop[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: PlantingPayload) => void
}

function PlantingForm({
  open,
  planting,
  crops,
  saving,
  error,
  onClose,
  onSave,
}: PlantingFormProps) {
  const [cropId, setCropId] = useState('')
  const [plantingDate, setPlantingDate] = useState('')
  const [expectedHarvestDate, setExpectedHarvestDate] = useState('')
  const [actualHarvestDate, setActualHarvestDate] = useState('')
  const [area, setArea] = useState('')

  useEffect(() => {
    if (!open) return
    setCropId(planting?.cropId ?? crops[0]?.id ?? '')
    setPlantingDate(planting?.plantingDate ?? new Date().toISOString().slice(0, 10))
    setExpectedHarvestDate(planting?.expectedHarvestDate ?? '')
    setActualHarvestDate(planting?.actualHarvestDate ?? '')
    setArea(planting?.areaHectares?.toString() ?? '')
  }, [open, planting, crops])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      cropId,
      plantingDate,
      expectedHarvestDate: expectedHarvestDate || null,
      ...(planting ? { actualHarvestDate: actualHarvestDate || null } : {}),
      areaHectares: area ? Number(area) : null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{planting ? 'Edit crop planting' : 'Plan a crop'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <FormControl required fullWidth>
              <InputLabel id="crop-select-label">Crop</InputLabel>
              <Select
                labelId="crop-select-label"
                label="Crop"
                value={cropId}
                onChange={(event) => setCropId(event.target.value)}
              >
                {crops.map((crop) => (
                  <MenuItem key={crop.id} value={crop.id}>
                    {crop.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Planting date"
              type="date"
              value={plantingDate}
              onChange={(event) => setPlantingDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              required
            />
            <TextField
              label="Expected harvest date"
              type="date"
              value={expectedHarvestDate}
              onChange={(event) => setExpectedHarvestDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            {planting && (
              <TextField
                label="Actual harvest date"
                type="date"
                value={actualHarvestDate}
                onChange={(event) => setActualHarvestDate(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                helperText="Required before changing status to Harvested"
              />
            )}
            <TextField
              label="Crop area"
              type="number"
              value={area}
              onChange={(event) => setArea(event.target.value)}
              slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
              helperText="Hectares; cannot exceed the field area"
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving || !crops.length}>
            {saving ? 'Saving…' : planting ? 'Save changes' : 'Plan crop'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

interface CatalogFormProps {
  open: boolean
  crop: Crop | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: Pick<Crop, 'name' | 'scientificName' | 'description'>) => void
}

function CatalogForm({ open, crop, saving, error, onClose, onSave }: CatalogFormProps) {
  const [name, setName] = useState('')
  const [scientificName, setScientificName] = useState('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    if (!open) return
    setName(crop?.name ?? '')
    setScientificName(crop?.scientificName ?? '')
    setDescription(crop?.description ?? '')
  }, [open, crop])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      name: name.trim(),
      scientificName: scientificName.trim() || null,
      description: description.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{crop ? 'Edit catalog crop' : 'Add catalog crop'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Crop name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 150 } }}
              autoFocus
              required
            />
            <TextField
              label="Scientific name"
              value={scientificName}
              onChange={(event) => setScientificName(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 200 } }}
            />
            <TextField
              label="Description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 500 } }}
              multiline
              minRows={3}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Saving…' : crop ? 'Save changes' : 'Add crop'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function CropsPage() {
  const queryClient = useQueryClient()
  const { isAdministrator } = useAuth()
  const { selectedFarmId, selectedFarm } = useFarm()
  const [tab, setTab] = useState(0)
  const [selectedFieldId, setSelectedFieldId] = useState('')
  const [plantingOpen, setPlantingOpen] = useState(false)
  const [editingPlanting, setEditingPlanting] = useState<FieldCrop | null>(null)
  const [plantingError, setPlantingError] = useState('')
  const [catalogOpen, setCatalogOpen] = useState(false)
  const [editingCrop, setEditingCrop] = useState<Crop | null>(null)
  const [catalogError, setCatalogError] = useState('')

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const catalogQuery = useQuery({
    queryKey: ['crop-catalog', selectedFarmId],
    queryFn: () => apiRequest<Crop[]>(`/api/farms/${selectedFarmId}/crops/catalog`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const activeFields = useMemo(
    () => (fieldsQuery.data ?? []).filter((field) => field.isActive),
    [fieldsQuery.data],
  )
  const effectiveFieldId = activeFields.some((field) => field.id === selectedFieldId)
    ? selectedFieldId
    : (activeFields[0]?.id ?? '')
  const selectedField = activeFields.find((field) => field.id === effectiveFieldId)

  const plantingsQuery = useQuery({
    queryKey: ['field-crops', selectedFarmId, effectiveFieldId],
    queryFn: () =>
      apiRequest<FieldCrop[]>(
        `/api/farms/${selectedFarmId}/fields/${effectiveFieldId}/crops`,
      ),
    enabled: Boolean(selectedFarmId && effectiveFieldId),
    retry: false,
  })

  const plantingMutation = useMutation({
    mutationFn: (payload: PlantingPayload) =>
      apiRequest<FieldCrop>(
        editingPlanting
          ? `/api/farms/${selectedFarmId}/fields/${effectiveFieldId}/crops/${editingPlanting.id}`
          : `/api/farms/${selectedFarmId}/fields/${effectiveFieldId}/crops`,
        {
          method: editingPlanting ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['field-crops', selectedFarmId, effectiveFieldId],
      })
      setPlantingOpen(false)
      setEditingPlanting(null)
    },
    onError: (caught) =>
      setPlantingError(
        caught instanceof ApiError ? caught.message : 'Could not save the crop planting.',
      ),
  })

  const statusMutation = useMutation({
    mutationFn: ({ planting, status }: { planting: FieldCrop; status: FieldCropStatus }) =>
      apiRequest(
        `/api/farms/${selectedFarmId}/fields/${effectiveFieldId}/crops/${planting.id}/status`,
        { method: 'PATCH', body: JSON.stringify({ status }) },
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['field-crops', selectedFarmId, effectiveFieldId],
      }),
  })

  const catalogMutation = useMutation({
    mutationFn: (payload: Pick<Crop, 'name' | 'scientificName' | 'description'>) =>
      apiRequest<Crop>(editingCrop ? `/api/crops/${editingCrop.id}` : '/api/crops', {
        method: editingCrop ? 'PUT' : 'POST',
        body: JSON.stringify(payload),
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['crop-catalog', selectedFarmId] })
      setCatalogOpen(false)
      setEditingCrop(null)
    },
    onError: (caught) =>
      setCatalogError(caught instanceof ApiError ? caught.message : 'Could not save the crop.'),
  })

  const deactivateMutation = useMutation({
    mutationFn: (crop: Crop) =>
      apiRequest(`/api/crops/${crop.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: false }),
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['crop-catalog', selectedFarmId] }),
  })

  const openNewPlanting = () => {
    setEditingPlanting(null)
    setPlantingError('')
    setPlantingOpen(true)
  }

  const openEditPlanting = (planting: FieldCrop) => {
    setEditingPlanting(planting)
    setPlantingError('')
    setPlantingOpen(true)
  }

  const openCatalogForm = (crop: Crop | null) => {
    setEditingCrop(crop)
    setCatalogError('')
    setCatalogOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view crop planning.</Alert>
  }

  const pageError =
    fieldsQuery.error ?? catalogQuery.error ?? plantingsQuery.error ?? statusMutation.error

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
          Crops
        </Typography>
        <Typography color="text.secondary">
          Plan field crops and browse the active catalog for {selectedFarm?.name}.
        </Typography>
      </Box>

      <Card variant="outlined">
        <Tabs
          value={tab}
          onChange={(_, value: number) => setTab(value)}
          sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Field plantings" />
          <Tab label="Crop catalog" />
        </Tabs>

        {pageError && (
          <Alert severity="error" sx={{ m: 2 }}>
            {pageError instanceof ApiError ? pageError.message : 'Could not complete the request.'}
          </Alert>
        )}

        {tab === 0 && (
          <Box>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={2}
              sx={{ p: 2.5, justifyContent: 'space-between', alignItems: { sm: 'center' } }}
            >
              <FormControl size="small" sx={{ minWidth: 240 }}>
                <InputLabel id="field-select-label">Field</InputLabel>
                <Select
                  labelId="field-select-label"
                  label="Field"
                  value={effectiveFieldId}
                  onChange={(event) => setSelectedFieldId(event.target.value)}
                  disabled={!activeFields.length}
                >
                  {activeFields.map((field) => (
                    <MenuItem key={field.id} value={field.id}>
                      {field.name} ({field.code})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                onClick={openNewPlanting}
                disabled={!effectiveFieldId || !catalogQuery.data?.length}
              >
                Plan crop
              </Button>
            </Stack>

            {fieldsQuery.isLoading || plantingsQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : !activeFields.length ? (
              <Alert severity="info" sx={{ m: 2 }}>
                Add an active field before planning crops.
              </Alert>
            ) : !plantingsQuery.data?.length ? (
              <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
                <SpaRoundedIcon color="primary" sx={{ fontSize: 48 }} />
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  No crops planned for {selectedField?.name}
                </Typography>
                <Typography color="text.secondary">
                  Add a planting record to track its dates, area, and status.
                </Typography>
              </Stack>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Crop</TableCell>
                      <TableCell>Planting</TableCell>
                      <TableCell>Expected harvest</TableCell>
                      <TableCell>Area</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell align="right">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {plantingsQuery.data.map((planting) => {
                      const currentStatus = statusName(planting.status)
                      return (
                        <TableRow key={planting.id} hover>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontWeight: 700 }}>
                              {planting.cropName}
                            </Typography>
                          </TableCell>
                          <TableCell>{planting.plantingDate}</TableCell>
                          <TableCell>{planting.expectedHarvestDate ?? '—'}</TableCell>
                          <TableCell>
                            {planting.areaHectares == null ? '—' : `${planting.areaHectares} ha`}
                          </TableCell>
                          <TableCell>
                            <Select
                              size="small"
                              value={currentStatus}
                              onChange={(event) =>
                                statusMutation.mutate({
                                  planting,
                                  status: event.target.value as FieldCropStatus,
                                })
                              }
                              renderValue={(value) => (
                                <Chip
                                  size="small"
                                  label={value}
                                  color={statusColor(value as FieldCropStatus)}
                                />
                              )}
                              disabled={statusMutation.isPending}
                              sx={{ minWidth: 132 }}
                            >
                              {statuses.map((status) => (
                                <MenuItem
                                  key={status}
                                  value={status}
                                  disabled={status === 'Harvested' && !planting.actualHarvestDate}
                                >
                                  {status}
                                </MenuItem>
                              ))}
                            </Select>
                          </TableCell>
                          <TableCell align="right">
                            <Tooltip title="Edit planting">
                              <IconButton
                                onClick={() => openEditPlanting(planting)}
                                aria-label={`Edit ${planting.cropName}`}
                              >
                                <EditRoundedIcon />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        )}

        {tab === 1 && (
          <Box>
            {isAdministrator && (
              <Stack direction="row" sx={{ p: 2.5, justifyContent: 'flex-end' }}>
                <Button
                  variant="contained"
                  startIcon={<AddRoundedIcon />}
                  onClick={() => openCatalogForm(null)}
                >
                  Add crop
                </Button>
              </Stack>
            )}
            {catalogQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : !catalogQuery.data?.length ? (
              <Alert severity="info" sx={{ m: 2 }}>
                The active crop catalog is empty.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Crop</TableCell>
                      <TableCell>Scientific name</TableCell>
                      <TableCell>Description</TableCell>
                      {isAdministrator && <TableCell align="right">Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {catalogQuery.data.map((crop) => (
                      <TableRow key={crop.id} hover>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {crop.name}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ fontStyle: 'italic' }}>
                          {crop.scientificName ?? '—'}
                        </TableCell>
                        <TableCell>{crop.description ?? '—'}</TableCell>
                        {isAdministrator && (
                          <TableCell align="right">
                            <Tooltip title="Edit crop">
                              <IconButton
                                onClick={() => openCatalogForm(crop)}
                                aria-label={`Edit ${crop.name}`}
                              >
                                <EditRoundedIcon />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Deactivate crop">
                              <IconButton
                                onClick={() => deactivateMutation.mutate(crop)}
                                aria-label={`Deactivate ${crop.name}`}
                                disabled={deactivateMutation.isPending}
                              >
                                <ToggleOffRoundedIcon />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        )}
      </Card>

      <PlantingForm
        open={plantingOpen}
        planting={editingPlanting}
        crops={catalogQuery.data ?? []}
        saving={plantingMutation.isPending}
        error={plantingError}
        onClose={() => setPlantingOpen(false)}
        onSave={(payload) => plantingMutation.mutate(payload)}
      />
      <CatalogForm
        open={catalogOpen}
        crop={editingCrop}
        saving={catalogMutation.isPending}
        error={catalogError}
        onClose={() => setCatalogOpen(false)}
        onSave={(payload) => catalogMutation.mutate(payload)}
      />
    </Stack>
  )
}
