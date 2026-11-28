import { useEffect, useState, type FormEvent } from 'react'
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
  Switch,
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
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import GridViewRoundedIcon from '@mui/icons-material/GridViewRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Crop, Field, Plot, PlotAreaUnit } from '../types/api'
import { plotAreaUnits } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from the selected record */

function areaUnitLabel(unit: PlotAreaUnit): string {
  return plotAreaUnits.find((option) => option.value === unit)?.label ?? unit
}

interface PlotPayload {
  fieldId: string
  plotNumber: string
  name: string
  areaValue: number
  areaUnit: PlotAreaUnit
  location: string
  cropId: string
  comment: string | null
}

interface PlotFormProps {
  open: boolean
  plot: Plot | null
  fields: Field[]
  crops: Crop[]
  defaultFieldId: string
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: PlotPayload) => void
}

function PlotForm({
  open,
  plot,
  fields,
  crops,
  defaultFieldId,
  saving,
  error,
  onClose,
  onSave,
}: PlotFormProps) {
  const [fieldId, setFieldId] = useState('')
  const [plotNumber, setPlotNumber] = useState('')
  const [name, setName] = useState('')
  const [area, setArea] = useState('')
  const [areaUnit, setAreaUnit] = useState<PlotAreaUnit>('Hectares')
  const [location, setLocation] = useState('')
  const [cropId, setCropId] = useState('')
  const [comment, setComment] = useState('')

  useEffect(() => {
    if (!open) return
    setFieldId(plot?.fieldId ?? defaultFieldId)
    setPlotNumber(plot?.plotNumber ?? '')
    setName(plot?.name ?? '')
    setArea(plot?.areaValue?.toString() ?? '')
    setAreaUnit(plot?.areaUnit ?? 'Hectares')
    setLocation(plot?.location ?? '')
    setCropId(plot?.cropId ?? crops[0]?.id ?? '')
    setComment(plot?.comment ?? '')
  }, [open, plot, defaultFieldId, crops])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      fieldId,
      plotNumber: plotNumber.trim(),
      name: name.trim(),
      areaValue: Number(area),
      areaUnit,
      location: location.trim(),
      cropId,
      comment: comment.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{plot ? 'Edit plot' : 'Add plot'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <FormControl required fullWidth>
              <InputLabel id="plot-field-label">Field</InputLabel>
              <Select
                labelId="plot-field-label"
                label="Field"
                value={fieldId}
                onChange={(event) => setFieldId(event.target.value)}
              >
                {fields.map((field) => (
                  <MenuItem key={field.id} value={field.id}>
                    {field.name} ({field.code})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Plot number"
              value={plotNumber}
              onChange={(event) => setPlotNumber(event.target.value)}
              slotProps={{ htmlInput: { minLength: 1, maxLength: 50 } }}
              autoFocus
              required
            />
            <TextField
              label="Plot name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 150 } }}
              required
            />
            <Box sx={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 2 }}>
              <TextField
                label="Area"
                value={area}
                onChange={(event) => setArea(event.target.value)}
                type="number"
                slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                required
              />
              <FormControl required fullWidth>
                <InputLabel id="plot-area-unit-label">Unit</InputLabel>
                <Select
                  labelId="plot-area-unit-label"
                  label="Unit"
                  value={areaUnit}
                  onChange={(event) => setAreaUnit(event.target.value as PlotAreaUnit)}
                >
                  {plotAreaUnits.map((option) => (
                    <MenuItem key={option.value} value={option.value}>
                      {option.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>
            <TextField
              label="Location"
              value={location}
              onChange={(event) => setLocation(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 300 } }}
              required
            />
            <FormControl required fullWidth>
              <InputLabel id="plot-crop-label">Crop type</InputLabel>
              <Select
                labelId="plot-crop-label"
                label="Crop type"
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
              label="Comment"
              value={comment}
              onChange={(event) => setComment(event.target.value)}
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
            disabled={saving || !fields.length || !crops.length}
          >
            {saving ? 'Saving…' : plot ? 'Save changes' : 'Add plot'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function PlotsPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canManage = hasPermission('plots.manage')
  const [fieldFilter, setFieldFilter] = useState('')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingPlot, setEditingPlot] = useState<Plot | null>(null)
  const [formError, setFormError] = useState('')
  const queryKey = ['plots', selectedFarmId, fieldFilter]

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const cropsQuery = useQuery({
    queryKey: ['crop-catalog', selectedFarmId],
    queryFn: () => apiRequest<Crop[]>(`/api/farms/${selectedFarmId}/crops/catalog`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const {
    data: plots = [],
    isLoading,
    error,
  } = useQuery({
    queryKey,
    queryFn: () =>
      apiRequest<Plot[]>(
        `/api/farms/${selectedFarmId}/plots${fieldFilter ? `?fieldId=${fieldFilter}` : ''}`,
      ),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: PlotPayload) =>
      apiRequest<Plot>(
        editingPlot
          ? `/api/farms/${selectedFarmId}/plots/${editingPlot.id}`
          : `/api/farms/${selectedFarmId}/plots`,
        {
          method: editingPlot ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['plots', selectedFarmId] })
      setDialogOpen(false)
      setEditingPlot(null)
    },
    onError: (caught) =>
      setFormError(caught instanceof ApiError ? caught.message : 'Could not save the plot.'),
  })

  const statusMutation = useMutation({
    mutationFn: (plot: Plot) =>
      apiRequest(`/api/farms/${selectedFarmId}/plots/${plot.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !plot.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['plots', selectedFarmId] }),
  })

  const activeFields = (fieldsQuery.data ?? []).filter((field) => field.isActive)
  const activeCrops = (cropsQuery.data ?? []).filter((crop) => crop.isActive)

  const openCreate = () => {
    setEditingPlot(null)
    setFormError('')
    setDialogOpen(true)
  }

  const openEdit = (plot: Plot) => {
    setEditingPlot(plot)
    setFormError('')
    setDialogOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view and manage plots.</Alert>
  }

  const pageError = fieldsQuery.error ?? cropsQuery.error ?? error ?? statusMutation.error

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Plots
          </Typography>
          <Typography color="text.secondary">
            Manage plots within fields for {selectedFarm?.name}.
          </Typography>
        </Box>
        {canManage && (
          <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={openCreate}>
            Add plot
          </Button>
        )}
      </Stack>

      {pageError && (
        <Alert severity="error">
          {pageError instanceof ApiError ? pageError.message : 'Could not load plots.'}
        </Alert>
      )}

      <Card variant="outlined">
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ p: 2.5, justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <FormControl size="small" sx={{ minWidth: 240 }}>
            <InputLabel id="plot-field-filter-label">Field</InputLabel>
            <Select
              labelId="plot-field-filter-label"
              label="Field"
              value={fieldFilter}
              onChange={(event) => setFieldFilter(event.target.value)}
              disabled={!activeFields.length}
            >
              <MenuItem value="">All fields</MenuItem>
              {activeFields.map((field) => (
                <MenuItem key={field.id} value={field.id}>
                  {field.name} ({field.code})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Typography variant="body2" color="text.secondary">
            {plots.length} plot{plots.length === 1 ? '' : 's'}
          </Typography>
        </Stack>

        {isLoading || fieldsQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : plots.length === 0 ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <GridViewRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No plots yet
            </Typography>
            <Typography color="text.secondary">
              Add the first plot to subdivide a field and record its crop type.
            </Typography>
            {canManage && (
              <Button startIcon={<AddRoundedIcon />} onClick={openCreate}>
                Add plot
              </Button>
            )}
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Plot</TableCell>
                  <TableCell>Field</TableCell>
                  <TableCell>Area</TableCell>
                  <TableCell>Location</TableCell>
                  <TableCell>Crop type</TableCell>
                  <TableCell>Comment</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {plots.map((plot) => (
                  <TableRow key={plot.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {plot.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Plot {plot.plotNumber}
                      </Typography>
                    </TableCell>
                    <TableCell>{plot.fieldName}</TableCell>
                    <TableCell>
                      {plot.areaValue} {areaUnitLabel(plot.areaUnit)}
                      {plot.areaUnit !== 'Hectares' && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {plot.areaHectares} ha
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>{plot.location}</TableCell>
                    <TableCell>
                      <Chip label={plot.cropName} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>{plot.comment ?? '—'}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Switch
                          size="small"
                          checked={plot.isActive}
                          onChange={() => statusMutation.mutate(plot)}
                          disabled={!canManage || statusMutation.isPending}
                          slotProps={{
                            input: { 'aria-label': `Toggle ${plot.name} status` },
                          }}
                        />
                        <Typography variant="body2">
                          {plot.isActive ? 'Active' : 'Inactive'}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell align="right">
                      {canManage && (
                        <Tooltip title="Edit plot">
                          <IconButton onClick={() => openEdit(plot)} aria-label={`Edit ${plot.name}`}>
                            <EditRoundedIcon />
                          </IconButton>
                        </Tooltip>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Card>

      <PlotForm
        open={dialogOpen}
        plot={editingPlot}
        fields={activeFields}
        crops={activeCrops}
        defaultFieldId={fieldFilter || activeFields[0]?.id || ''}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />
    </Stack>
  )
}
