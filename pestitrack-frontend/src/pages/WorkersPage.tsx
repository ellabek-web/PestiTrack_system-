import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Checkbox,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
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
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Worker } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog form resets from the selected worker */

type WorkerPayload = Omit<Worker, 'id' | 'farmId' | 'isActive' | 'createdAtUtc'>

interface WorkerFormProps {
  open: boolean
  worker: Worker | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: WorkerPayload) => void
}

function WorkerForm({ open, worker, saving, error, onClose, onSave }: WorkerFormProps) {
  const [fullName, setFullName] = useState('')
  const [workerCode, setWorkerCode] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [email, setEmail] = useState('')
  const [isApplicator, setIsApplicator] = useState(false)
  const [certificationNumber, setCertificationNumber] = useState('')
  const [certificationExpiryDate, setCertificationExpiryDate] = useState('')

  useEffect(() => {
    if (!open) return
    setFullName(worker?.fullName ?? '')
    setWorkerCode(worker?.workerCode ?? '')
    setPhoneNumber(worker?.phoneNumber ?? '')
    setEmail(worker?.email ?? '')
    setIsApplicator(worker?.isApplicator ?? false)
    setCertificationNumber(worker?.certificationNumber ?? '')
    setCertificationExpiryDate(worker?.certificationExpiryDate?.slice(0, 10) ?? '')
  }, [open, worker])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      fullName: fullName.trim(),
      workerCode: workerCode.trim(),
      phoneNumber: phoneNumber.trim() || null,
      email: email.trim() || null,
      isApplicator,
      certificationNumber: isApplicator ? certificationNumber.trim() || null : null,
      certificationExpiryDate:
        isApplicator && certificationExpiryDate ? certificationExpiryDate : null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{worker ? 'Edit worker' : 'Add worker'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Full name"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 200 } }}
              autoFocus
              required
            />
            <TextField
              label="Worker code"
              value={workerCode}
              onChange={(event) => setWorkerCode(event.target.value.toUpperCase())}
              slotProps={{
                htmlInput: { minLength: 2, maxLength: 50, pattern: '[A-Za-z0-9_-]+' },
              }}
              helperText="Letters, numbers, hyphens, and underscores"
              required
            />
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2.5,
              }}
            >
              <TextField
                label="Phone number"
                value={phoneNumber}
                onChange={(event) => setPhoneNumber(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 30 } }}
              />
              <TextField
                label="Email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 256 } }}
              />
            </Box>
            <FormControlLabel
              control={
                <Checkbox
                  checked={isApplicator}
                  onChange={(event) => setIsApplicator(event.target.checked)}
                />
              }
              label="Certified pesticide applicator"
            />
            {isApplicator && (
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                  gap: 2.5,
                }}
              >
                <TextField
                  label="Certification number"
                  value={certificationNumber}
                  onChange={(event) => setCertificationNumber(event.target.value)}
                  slotProps={{ htmlInput: { maxLength: 100 } }}
                />
                <TextField
                  label="Certification expiry"
                  type="date"
                  value={certificationExpiryDate}
                  onChange={(event) => setCertificationExpiryDate(event.target.value)}
                  slotProps={{ inputLabel: { shrink: true } }}
                />
              </Box>
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Saving…' : worker ? 'Save changes' : 'Add worker'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function WorkersPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm } = useFarm()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingWorker, setEditingWorker] = useState<Worker | null>(null)
  const [formError, setFormError] = useState('')
  const queryKey = ['workers', selectedFarmId]

  const { data: workers = [], isLoading, error } = useQuery({
    queryKey,
    queryFn: () => apiRequest<Worker[]>(`/api/farms/${selectedFarmId}/workers`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: WorkerPayload) =>
      apiRequest<Worker>(
        editingWorker
          ? `/api/farms/${selectedFarmId}/workers/${editingWorker.id}`
          : `/api/farms/${selectedFarmId}/workers`,
        {
          method: editingWorker ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey })
      setDialogOpen(false)
      setEditingWorker(null)
    },
    onError: (caught) =>
      setFormError(caught instanceof ApiError ? caught.message : 'Could not save the worker.'),
  })

  const statusMutation = useMutation({
    mutationFn: (worker: Worker) =>
      apiRequest<Worker>(`/api/farms/${selectedFarmId}/workers/${worker.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !worker.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })

  const openForm = (worker: Worker | null) => {
    setEditingWorker(worker)
    setFormError('')
    setDialogOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to manage workers.</Alert>
  }

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Workers
          </Typography>
          <Typography color="text.secondary">
            Manage workers and applicator certifications for {selectedFarm?.name}.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => openForm(null)}>
          Add worker
        </Button>
      </Stack>

      {(error || statusMutation.error) && (
        <Alert severity="error">
          {(error ?? statusMutation.error) instanceof ApiError
            ? ((error ?? statusMutation.error) as ApiError).message
            : 'Could not complete the worker request.'}
        </Alert>
      )}

      <Card variant="outlined">
        {isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !workers.length ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <PeopleRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No workers yet
            </Typography>
            <Typography color="text.secondary">
              Add workers before recording pesticide applications.
            </Typography>
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Worker</TableCell>
                  <TableCell>Contact</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Certification</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {workers.map((worker) => {
                  const expired =
                    worker.certificationExpiryDate != null &&
                    new Date(worker.certificationExpiryDate) < new Date()
                  return (
                    <TableRow key={worker.id} hover>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {worker.fullName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {worker.workerCode}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{worker.email ?? '—'}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {worker.phoneNumber}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={worker.isApplicator ? 'Applicator' : 'Worker'}
                          size="small"
                          color={worker.isApplicator ? 'primary' : 'default'}
                        />
                      </TableCell>
                      <TableCell>
                        {worker.isApplicator ? (
                          <Stack spacing={0.5}>
                            <Typography variant="body2">
                              {worker.certificationNumber ?? 'No number'}
                            </Typography>
                            {worker.certificationExpiryDate && (
                              <Chip
                                size="small"
                                label={`${expired ? 'Expired' : 'Expires'} ${new Date(
                                  worker.certificationExpiryDate,
                                ).toLocaleDateString()}`}
                                color={expired ? 'error' : 'success'}
                                variant="outlined"
                              />
                            )}
                          </Stack>
                        ) : (
                          '—'
                        )}
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <Switch
                            size="small"
                            checked={worker.isActive}
                            onChange={() => statusMutation.mutate(worker)}
                            disabled={statusMutation.isPending}
                            slotProps={{
                              input: { 'aria-label': `Toggle ${worker.fullName} status` },
                            }}
                          />
                          <Typography variant="body2">
                            {worker.isActive ? 'Active' : 'Inactive'}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="Edit worker">
                          <IconButton
                            onClick={() => openForm(worker)}
                            aria-label={`Edit ${worker.fullName}`}
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
      </Card>

      <WorkerForm
        open={dialogOpen}
        worker={editingWorker}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />
    </Stack>
  )
}
