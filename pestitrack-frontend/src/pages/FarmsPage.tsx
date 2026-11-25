import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
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
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Farm } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog form resets from the selected farm */

export function FarmsPage() {
  const queryClient = useQueryClient()
  const { farms } = useFarm()
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Farm | null>(null)
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [location, setLocation] = useState('')
  const [latitude, setLatitude] = useState('')
  const [longitude, setLongitude] = useState('')
  const [formError, setFormError] = useState('')
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    if (!open) return
    setName(editing?.name ?? '')
    setCode(editing?.code ?? '')
    setLocation(editing?.location ?? '')
    setLatitude(editing?.latitude != null ? String(editing.latitude) : '')
    setLongitude(editing?.longitude != null ? String(editing.longitude) : '')
  }, [open, editing])

  const saveMutation = useMutation({
    mutationFn: () =>
      apiRequest<Farm>(editing ? `/api/farms/${editing.id}` : '/api/farms', {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify({
          name: name.trim(),
          ...(editing ? {} : { code: code.trim() }),
          location: location.trim() || null,
          latitude: latitude.trim() === '' ? null : Number(latitude),
          longitude: longitude.trim() === '' ? null : Number(longitude),
        }),
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['farms'] })
      setOpen(false)
    },
    onError: (error) =>
      setFormError(error instanceof ApiError ? error.message : 'Could not save this farm.'),
  })

  const statusMutation = useMutation({
    mutationFn: (farm: Farm) =>
      apiRequest(`/api/farms/${farm.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !farm.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['farms'] }),
    onError: (error) =>
      setActionError(error instanceof ApiError ? error.message : 'Could not change farm status.'),
  })

  const showDialog = (farm: Farm | null) => {
    setEditing(farm)
    setFormError('')
    setOpen(true)
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    saveMutation.mutate()
  }

  return (
    <Stack spacing={3}>
      <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 2 }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>Farms</Typography>
          <Typography color="text.secondary">Create farms and manage their operational status.</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => showDialog(null)}>Create farm</Button>
      </Stack>

      {actionError && <Alert severity="error">{actionError}</Alert>}
      <Card variant="outlined">
        <TableContainer>
          <Table>
            <TableHead><TableRow><TableCell>Farm</TableCell><TableCell>Code</TableCell><TableCell>Location</TableCell><TableCell>Created</TableCell><TableCell>Status</TableCell><TableCell align="right">Actions</TableCell></TableRow></TableHead>
            <TableBody>
              {farms.map((farm) => (
                <TableRow key={farm.id} hover>
                  <TableCell sx={{ fontWeight: 700 }}>{farm.name}</TableCell>
                  <TableCell>{farm.code}</TableCell>
                  <TableCell>
                    <Typography variant="body2">{farm.location ?? '—'}</Typography>
                    {farm.latitude != null && farm.longitude != null && (
                      <Typography variant="caption" color="text.secondary">
                        {farm.latitude.toFixed(4)}, {farm.longitude.toFixed(4)}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{new Date(farm.createdAtUtc).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      <Switch checked={farm.isActive} onChange={() => statusMutation.mutate(farm)} disabled={statusMutation.isPending} />
                      <Typography variant="body2">{farm.isActive ? 'Active' : 'Inactive'}</Typography>
                    </Stack>
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Edit farm"><IconButton onClick={() => showDialog(farm)}><EditRoundedIcon /></IconButton></Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <Dialog open={open} onClose={saveMutation.isPending ? undefined : () => setOpen(false)} fullWidth maxWidth="sm">
        <Box component="form" onSubmit={submit}>
          <DialogTitle>{editing ? 'Edit farm' : 'Create farm'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2.5} sx={{ pt: 1 }}>
              {formError && <Alert severity="error">{formError}</Alert>}
              <TextField label="Farm name" value={name} onChange={(event) => setName(event.target.value)} required />
              <TextField label="Farm code" value={code} onChange={(event) => setCode(event.target.value.toUpperCase())} disabled={Boolean(editing)} required={!editing} />
              <TextField label="Location" value={location} onChange={(event) => setLocation(event.target.value)} />
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                <TextField
                  label="Latitude (optional)"
                  type="number"
                  value={latitude}
                  onChange={(event) => setLatitude(event.target.value)}
                  slotProps={{ htmlInput: { min: -90, max: 90, step: 'any' } }}
                  helperText="Used for live weather sync"
                />
                <TextField
                  label="Longitude (optional)"
                  type="number"
                  value={longitude}
                  onChange={(event) => setLongitude(event.target.value)}
                  slotProps={{ htmlInput: { min: -180, max: 180, step: 'any' } }}
                />
              </Box>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 3 }}>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saveMutation.isPending}>{saveMutation.isPending ? 'Saving…' : 'Save farm'}</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Stack>
  )
}
