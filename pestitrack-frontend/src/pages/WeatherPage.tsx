import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
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
import CloudSyncRoundedIcon from '@mui/icons-material/CloudSyncRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import WaterDropRoundedIcon from '@mui/icons-material/WaterDropRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Field, WeatherRecord } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog form resets from the selected record */

type WeatherPayload = Omit<WeatherRecord, 'id' | 'farmId' | 'createdAtUtc'>

function toLocalInput(value: string): string {
  const date = new Date(value)
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function optionalNumber(value: string): number | null {
  return value === '' ? null : Number(value)
}

interface WeatherFormProps {
  open: boolean
  record: WeatherRecord | null
  fields: Field[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: WeatherPayload) => void
}

function WeatherForm({
  open,
  record,
  fields,
  saving,
  error,
  onClose,
  onSave,
}: WeatherFormProps) {
  const [recordedAt, setRecordedAt] = useState('')
  const [fieldId, setFieldId] = useState('')
  const [temperature, setTemperature] = useState('')
  const [humidity, setHumidity] = useState('')
  const [rainfall, setRainfall] = useState('')
  const [windSpeed, setWindSpeed] = useState('')
  const [windDirection, setWindDirection] = useState('')
  const [conditions, setConditions] = useState('')
  const [source, setSource] = useState('')
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (!open) return
    setRecordedAt(toLocalInput(record?.recordedAtUtc ?? new Date().toISOString()))
    setFieldId(record?.fieldId ?? '')
    setTemperature(record?.temperatureCelsius?.toString() ?? '')
    setHumidity(record?.humidityPercentage?.toString() ?? '')
    setRainfall(record?.rainfallMillimeters?.toString() ?? '')
    setWindSpeed(record?.windSpeedMetersPerSecond?.toString() ?? '')
    setWindDirection(record?.windDirectionDegrees?.toString() ?? '')
    setConditions(record?.conditions ?? '')
    setSource(record?.source ?? 'Manual')
    setNotes(record?.notes ?? '')
  }, [open, record])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      recordedAtUtc: new Date(recordedAt).toISOString(),
      fieldId: fieldId || null,
      temperatureCelsius: optionalNumber(temperature),
      humidityPercentage: optionalNumber(humidity),
      rainfallMillimeters: optionalNumber(rainfall),
      windSpeedMetersPerSecond: optionalNumber(windSpeed),
      windDirectionDegrees: optionalNumber(windDirection),
      conditions: conditions.trim() || null,
      source: source.trim() || 'Manual',
      notes: notes.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="md">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{record ? 'Edit weather record' : 'Record weather'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2.5,
              }}
            >
              <TextField
                label="Recorded at"
                type="datetime-local"
                value={recordedAt}
                onChange={(event) => setRecordedAt(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                required
              />
              <FormControl>
                <InputLabel id="weather-field-label">Field</InputLabel>
                <Select
                  labelId="weather-field-label"
                  label="Field"
                  value={fieldId}
                  onChange={(event) => setFieldId(event.target.value)}
                >
                  <MenuItem value="">Whole farm</MenuItem>
                  {fields
                    .filter((field) => field.isActive)
                    .map((field) => (
                      <MenuItem key={field.id} value={field.id}>
                        {field.name}
                      </MenuItem>
                    ))}
                </Select>
              </FormControl>
              <TextField
                label="Temperature"
                type="number"
                value={temperature}
                onChange={(event) => setTemperature(event.target.value)}
                slotProps={{ htmlInput: { min: -100, max: 100, step: 0.1 } }}
                helperText="°C"
              />
              <TextField
                label="Humidity"
                type="number"
                value={humidity}
                onChange={(event) => setHumidity(event.target.value)}
                slotProps={{ htmlInput: { min: 0, max: 100, step: 0.1 } }}
                helperText="%"
              />
              <TextField
                label="Rainfall"
                type="number"
                value={rainfall}
                onChange={(event) => setRainfall(event.target.value)}
                slotProps={{ htmlInput: { min: 0, step: 0.1 } }}
                helperText="mm"
              />
              <TextField
                label="Wind speed"
                type="number"
                value={windSpeed}
                onChange={(event) => setWindSpeed(event.target.value)}
                slotProps={{ htmlInput: { min: 0, step: 0.1 } }}
                helperText="m/s"
              />
              <TextField
                label="Wind direction"
                type="number"
                value={windDirection}
                onChange={(event) => setWindDirection(event.target.value)}
                slotProps={{ htmlInput: { min: 0, max: 360, step: 1 } }}
                helperText="Degrees (0–360)"
              />
              <TextField
                label="Conditions"
                value={conditions}
                onChange={(event) => setConditions(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
                placeholder="Clear, cloudy, rain…"
              />
              <TextField
                label="Source"
                value={source}
                onChange={(event) => setSource(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
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
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Saving…' : record ? 'Save changes' : 'Record weather'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function WeatherPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canManage = hasPermission('weather.manage')
  const [fieldFilter, setFieldFilter] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<WeatherRecord | null>(null)
  const [formError, setFormError] = useState('')
  const [syncOpen, setSyncOpen] = useState(false)
  const [syncFieldId, setSyncFieldId] = useState('')
  const [syncLatitude, setSyncLatitude] = useState('')
  const [syncLongitude, setSyncLongitude] = useState('')
  const [syncNotes, setSyncNotes] = useState('')
  const [syncError, setSyncError] = useState('')

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const params = new URLSearchParams()
  if (fieldFilter) params.set('fieldId', fieldFilter)
  if (fromDate) params.set('fromUtc', new Date(`${fromDate}T00:00:00`).toISOString())
  if (toDate) params.set('toUtc', new Date(`${toDate}T23:59:59`).toISOString())
  const queryString = params.toString()
  const queryKey = ['weather', selectedFarmId, fieldFilter, fromDate, toDate]

  const weatherQuery = useQuery({
    queryKey,
    queryFn: () =>
      apiRequest<WeatherRecord[]>(
        `/api/farms/${selectedFarmId}/weather-records${queryString ? `?${queryString}` : ''}`,
      ),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: WeatherPayload) =>
      apiRequest<WeatherRecord>(
        editingRecord
          ? `/api/farms/${selectedFarmId}/weather-records/${editingRecord.id}`
          : `/api/farms/${selectedFarmId}/weather-records`,
        {
          method: editingRecord ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['weather', selectedFarmId] })
      setDialogOpen(false)
      setEditingRecord(null)
    },
    onError: (caught) =>
      setFormError(
        caught instanceof ApiError ? caught.message : 'Could not save the weather record.',
      ),
  })

  const openForm = (record: WeatherRecord | null) => {
    setEditingRecord(record)
    setFormError('')
    setDialogOpen(true)
  }

  const syncMutation = useMutation({
    mutationFn: () =>
      apiRequest<WeatherRecord>(`/api/farms/${selectedFarmId}/weather-records/sync`, {
        method: 'POST',
        body: JSON.stringify({
          fieldId: syncFieldId || null,
          latitude: syncLatitude.trim() === '' ? null : Number(syncLatitude),
          longitude: syncLongitude.trim() === '' ? null : Number(syncLongitude),
          notes: syncNotes.trim() || null,
        }),
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['weather', selectedFarmId] })
      setSyncOpen(false)
    },
    onError: (caught) =>
      setSyncError(
        caught instanceof ApiError ? caught.message : 'Could not sync live weather.',
      ),
  })

  const openSyncDialog = () => {
    setSyncFieldId('')
    setSyncLatitude(selectedFarm?.latitude != null ? String(selectedFarm.latitude) : '')
    setSyncLongitude(selectedFarm?.longitude != null ? String(selectedFarm.longitude) : '')
    setSyncNotes('')
    setSyncError('')
    setSyncOpen(true)
  }

  const fieldName = (fieldId: string | null) =>
    fieldsQuery.data?.find((field) => field.id === fieldId)?.name ?? 'Whole farm'

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view weather records.</Alert>
  }

  const pageError = fieldsQuery.error ?? weatherQuery.error

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Weather
          </Typography>
          <Typography color="text.secondary">
            Record weather conditions for {selectedFarm?.name}.
          </Typography>
        </Box>
        {canManage && (
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              startIcon={<CloudSyncRoundedIcon />}
              onClick={openSyncDialog}
            >
              Sync live weather
            </Button>
            <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => openForm(null)}>
              Record weather
            </Button>
          </Stack>
        )}
      </Stack>

      {pageError && (
        <Alert severity="error">
          {pageError instanceof ApiError ? pageError.message : 'Could not load weather records.'}
        </Alert>
      )}

      <Card variant="outlined">
        <Box
          sx={{
            p: 2.5,
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: '2fr 1fr 1fr' },
            gap: 2,
          }}
        >
          <FormControl size="small">
            <InputLabel id="weather-filter-label">Field</InputLabel>
            <Select
              labelId="weather-filter-label"
              label="Field"
              value={fieldFilter}
              onChange={(event) => setFieldFilter(event.target.value)}
            >
              <MenuItem value="">All fields</MenuItem>
              {(fieldsQuery.data ?? []).map((field) => (
                <MenuItem key={field.id} value={field.id}>
                  {field.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="From"
            type="date"
            size="small"
            value={fromDate}
            onChange={(event) => setFromDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="To"
            type="date"
            size="small"
            value={toDate}
            onChange={(event) => setToDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Box>

        {weatherQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !weatherQuery.data?.length ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <WaterDropRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No weather records
            </Typography>
            <Typography color="text.secondary">
              Record current conditions or change the filters.
            </Typography>
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Recorded at</TableCell>
                  <TableCell>Location</TableCell>
                  <TableCell>Conditions</TableCell>
                  <TableCell>Temperature</TableCell>
                  <TableCell>Humidity</TableCell>
                  <TableCell>Rainfall</TableCell>
                  <TableCell>Wind</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {weatherQuery.data.map((record) => (
                  <TableRow key={record.id} hover>
                    <TableCell>{new Date(record.recordedAtUtc).toLocaleString()}</TableCell>
                    <TableCell>{fieldName(record.fieldId)}</TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {record.conditions ?? '—'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {record.source}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      {record.temperatureCelsius == null
                        ? '—'
                        : `${record.temperatureCelsius} °C`}
                    </TableCell>
                    <TableCell>
                      {record.humidityPercentage == null
                        ? '—'
                        : `${record.humidityPercentage}%`}
                    </TableCell>
                    <TableCell>
                      {record.rainfallMillimeters == null
                        ? '—'
                        : `${record.rainfallMillimeters} mm`}
                    </TableCell>
                    <TableCell>
                      {record.windSpeedMetersPerSecond == null
                        ? '—'
                        : `${record.windSpeedMetersPerSecond} m/s`}
                    </TableCell>
                    <TableCell align="right">
                      {canManage && (
                        <Tooltip title="Edit weather record">
                          <IconButton
                            onClick={() => openForm(record)}
                            aria-label="Edit weather record"
                          >
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

      <WeatherForm
        open={dialogOpen}
        record={editingRecord}
        fields={fieldsQuery.data ?? []}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />

      <Dialog
        open={syncOpen}
        onClose={syncMutation.isPending ? undefined : () => setSyncOpen(false)}
        fullWidth
        maxWidth="sm"
      >
        <Box
          component="form"
          onSubmit={(event) => {
            event.preventDefault()
            syncMutation.mutate()
          }}
        >
          <DialogTitle>Sync live weather (Open-Meteo)</DialogTitle>
          <DialogContent>
            <Stack spacing={2.5} sx={{ pt: 1 }}>
              {syncError && <Alert severity="error">{syncError}</Alert>}
              <Alert severity="info">
                Fetches current temperature, humidity, precipitation, wind speed, and wind direction from Open-Meteo and saves a new weather record.
              </Alert>
              <FormControl fullWidth>
                <InputLabel id="sync-field-label">Field (optional)</InputLabel>
                <Select
                  labelId="sync-field-label"
                  label="Field (optional)"
                  value={syncFieldId}
                  onChange={(event) => setSyncFieldId(event.target.value)}
                >
                  <MenuItem value="">Whole farm</MenuItem>
                  {(fieldsQuery.data ?? [])
                    .filter((field) => field.isActive)
                    .map((field) => (
                      <MenuItem key={field.id} value={field.id}>
                        {field.name}
                      </MenuItem>
                    ))}
                </Select>
              </FormControl>
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                <TextField
                  label="Latitude"
                  type="number"
                  value={syncLatitude}
                  onChange={(event) => setSyncLatitude(event.target.value)}
                  slotProps={{ htmlInput: { min: -90, max: 90, step: 'any' } }}
                  required
                />
                <TextField
                  label="Longitude"
                  type="number"
                  value={syncLongitude}
                  onChange={(event) => setSyncLongitude(event.target.value)}
                  slotProps={{ htmlInput: { min: -180, max: 180, step: 'any' } }}
                  required
                />
              </Box>
              <TextField
                label="Notes (optional)"
                value={syncNotes}
                onChange={(event) => setSyncNotes(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 1000 } }}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 3, pt: 1 }}>
            <Button onClick={() => setSyncOpen(false)} disabled={syncMutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" disabled={syncMutation.isPending}>
              {syncMutation.isPending ? 'Syncing…' : 'Fetch & save weather'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Stack>
  )
}
