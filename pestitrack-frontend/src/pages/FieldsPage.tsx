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
import GrassRoundedIcon from '@mui/icons-material/GrassRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Field } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from the selected record */

interface FieldPayload {
  name: string
  code: string
  areaHectares: number | null
  description: string | null
}

interface FieldFormProps {
  open: boolean
  field: Field | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: FieldPayload) => void
}

function FieldForm({ open, field, saving, error, onClose, onSave }: FieldFormProps) {
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [area, setArea] = useState('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    if (!open) return
    setName(field?.name ?? '')
    setCode(field?.code ?? '')
    setArea(field?.areaHectares?.toString() ?? '')
    setDescription(field?.description ?? '')
  }, [open, field])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      name: name.trim(),
      code: code.trim(),
      areaHectares: area ? Number(area) : null,
      description: description.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{field ? 'Edit field' : 'Add field'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Field name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 150 } }}
              autoFocus
              required
            />
            <TextField
              label="Field code"
              value={code}
              onChange={(event) => setCode(event.target.value.toUpperCase())}
              helperText="Letters, numbers, hyphens, and underscores"
              slotProps={{
                htmlInput: { minLength: 2, maxLength: 50, pattern: '[A-Za-z0-9_-]+' },
              }}
              required
            />
            <TextField
              label="Area"
              value={area}
              onChange={(event) => setArea(event.target.value)}
              type="number"
              slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
              helperText="Hectares"
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
            {saving ? 'Saving…' : field ? 'Save changes' : 'Add field'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function FieldsPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm } = useFarm()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingField, setEditingField] = useState<Field | null>(null)
  const [formError, setFormError] = useState('')
  const queryKey = ['fields', selectedFarmId]

  const {
    data: fields = [],
    isLoading,
    error,
  } = useQuery({
    queryKey,
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: FieldPayload) =>
      apiRequest<Field>(
        editingField
          ? `/api/farms/${selectedFarmId}/fields/${editingField.id}`
          : `/api/farms/${selectedFarmId}/fields`,
        {
          method: editingField ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey })
      setDialogOpen(false)
      setEditingField(null)
    },
    onError: (caught) =>
      setFormError(caught instanceof ApiError ? caught.message : 'Could not save the field.'),
  })

  const statusMutation = useMutation({
    mutationFn: (field: Field) =>
      apiRequest(`/api/farms/${selectedFarmId}/fields/${field.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !field.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })

  const openCreate = () => {
    setEditingField(null)
    setFormError('')
    setDialogOpen(true)
  }

  const openEdit = (field: Field) => {
    setEditingField(field)
    setFormError('')
    setDialogOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view and manage fields.</Alert>
  }

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Fields
          </Typography>
          <Typography color="text.secondary">
            Manage land areas for {selectedFarm?.name}.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={openCreate}>
          Add field
        </Button>
      </Stack>

      {statusMutation.error && (
        <Alert severity="error">
          {statusMutation.error instanceof ApiError
            ? statusMutation.error.message
            : 'Could not update field status.'}
        </Alert>
      )}
      {error && (
        <Alert severity="error">
          {error instanceof ApiError ? error.message : 'Could not load fields.'}
        </Alert>
      )}

      <Card variant="outlined">
        {isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : fields.length === 0 ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <GrassRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No fields yet
            </Typography>
            <Typography color="text.secondary">
              Add the first field to start planning crops and applications.
            </Typography>
            <Button startIcon={<AddRoundedIcon />} onClick={openCreate}>
              Add field
            </Button>
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Field</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Area</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {fields.map((field) => (
                  <TableRow key={field.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {field.name}
                      </Typography>
                      {field.description && (
                        <Typography variant="caption" color="text.secondary">
                          {field.description}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Chip label={field.code} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>
                      {field.areaHectares == null ? '—' : `${field.areaHectares} ha`}
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Switch
                          size="small"
                          checked={field.isActive}
                          onChange={() => statusMutation.mutate(field)}
                          disabled={statusMutation.isPending}
                          slotProps={{
                            input: { 'aria-label': `Toggle ${field.name} status` },
                          }}
                        />
                        <Typography variant="body2">
                          {field.isActive ? 'Active' : 'Inactive'}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="Edit field">
                        <IconButton onClick={() => openEdit(field)} aria-label={`Edit ${field.name}`}>
                          <EditRoundedIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Card>

      <FieldForm
        open={dialogOpen}
        field={editingField}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />
    </Stack>
  )
}
