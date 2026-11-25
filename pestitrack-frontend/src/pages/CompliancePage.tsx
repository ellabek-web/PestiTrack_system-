import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
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
  Pagination,
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
import CheckCircleOutlineRoundedIcon from '@mui/icons-material/CheckCircleOutlineRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import HealthAndSafetyRoundedIcon from '@mui/icons-material/HealthAndSafetyRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type {
  ComplianceOverview,
  ComplianceRecord,
  ComplianceRecordStatus,
  ComplianceRecordType,
  ComplianceSeverity,
  Field,
  PagedResponse,
  Worker,
} from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from selected record */

const recordTypes: ComplianceRecordType[] = [
  'Inspection',
  'Incident',
  'AuditFinding',
  'CertificationReview',
  'RegulatoryCheck',
]

const recordStatuses: ComplianceRecordStatus[] = [
  'Open',
  'InReview',
  'Resolved',
  'NonCompliant',
]

const recordSeverities: ComplianceSeverity[] = [
  'Low',
  'Medium',
  'High',
  'Critical',
]

function formatLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function statusChipColor(status: string): 'success' | 'warning' | 'error' | 'info' | 'default' {
  switch (status) {
    case 'Valid':
    case 'Resolved':
      return 'success'
    case 'ExpiringSoon':
    case 'InReview':
      return 'warning'
    case 'Expired':
    case 'Missing':
    case 'NonCompliant':
      return 'error'
    case 'Open':
      return 'info'
    default:
      return 'default'
  }
}

function severityChipColor(severity: string): 'error' | 'warning' | 'info' | 'default' {
  switch (severity) {
    case 'Critical':
    case 'High':
      return 'error'
    case 'Medium':
      return 'warning'
    case 'Low':
      return 'info'
    default:
      return 'default'
  }
}

function toDatetimeLocal(value: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

interface RecordPayload {
  fieldId: string | null
  workerId: string | null
  pesticideApplicationId: string | null
  recordType: ComplianceRecordType
  title: string
  status: ComplianceRecordStatus
  severity: ComplianceSeverity
  checkedAtUtc: string
  dueDateUtc: string | null
  inspectorName: string | null
  findings: string | null
  correctiveAction: string | null
}

interface RecordFormDialogProps {
  open: boolean
  record: ComplianceRecord | null
  fields: Field[]
  workers: Worker[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: RecordPayload) => void
}

function RecordFormDialog({
  open,
  record,
  fields,
  workers,
  saving,
  error,
  onClose,
  onSave,
}: RecordFormDialogProps) {
  const [title, setTitle] = useState('')
  const [recordType, setRecordType] = useState<ComplianceRecordType>('Inspection')
  const [status, setStatus] = useState<ComplianceRecordStatus>('Open')
  const [severity, setSeverity] = useState<ComplianceSeverity>('Medium')
  const [fieldId, setFieldId] = useState('')
  const [workerId, setWorkerId] = useState('')
  const [checkedAtLocal, setCheckedAtLocal] = useState('')
  const [dueDateLocal, setDueDateLocal] = useState('')
  const [inspectorName, setInspectorName] = useState('')
  const [findings, setFindings] = useState('')
  const [correctiveAction, setCorrectiveAction] = useState('')

  useEffect(() => {
    if (!open) return
    if (record) {
      setTitle(record.title)
      setRecordType((record.recordType as ComplianceRecordType) || 'Inspection')
      setStatus((record.status as ComplianceRecordStatus) || 'Open')
      setSeverity((record.severity as ComplianceSeverity) || 'Medium')
      setFieldId(record.fieldId ?? '')
      setWorkerId(record.workerId ?? '')
      setCheckedAtLocal(toDatetimeLocal(record.checkedAtUtc))
      setDueDateLocal(toDatetimeLocal(record.dueDateUtc))
      setInspectorName(record.inspectorName ?? '')
      setFindings(record.findings ?? '')
      setCorrectiveAction(record.correctiveAction ?? '')
    } else {
      setTitle('')
      setRecordType('Inspection')
      setStatus('Open')
      setSeverity('Medium')
      setFieldId('')
      setWorkerId('')
      setCheckedAtLocal(toDatetimeLocal(new Date().toISOString()))
      setDueDateLocal('')
      setInspectorName('')
      setFindings('')
      setCorrectiveAction('')
    }
  }, [open, record])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      fieldId: fieldId || null,
      workerId: workerId || null,
      pesticideApplicationId: record?.pesticideApplicationId ?? null,
      recordType,
      title: title.trim(),
      status,
      severity,
      checkedAtUtc: checkedAtLocal ? new Date(checkedAtLocal).toISOString() : new Date().toISOString(),
      dueDateUtc: dueDateLocal ? new Date(dueDateLocal).toISOString() : null,
      inspectorName: inspectorName.trim() || null,
      findings: findings.trim() || null,
      correctiveAction: correctiveAction.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="md">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{record ? 'Edit compliance record' : 'New compliance record'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              slotProps={{ htmlInput: { minLength: 3, maxLength: 200 } }}
              required
              autoFocus
            />
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' },
                gap: 2,
              }}
            >
              <FormControl fullWidth>
                <InputLabel id="record-type-label">Type</InputLabel>
                <Select
                  labelId="record-type-label"
                  label="Type"
                  value={recordType}
                  onChange={(event) => setRecordType(event.target.value as ComplianceRecordType)}
                >
                  {recordTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {formatLabel(type)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel id="record-status-label">Status</InputLabel>
                <Select
                  labelId="record-status-label"
                  label="Status"
                  value={status}
                  onChange={(event) => setStatus(event.target.value as ComplianceRecordStatus)}
                >
                  {recordStatuses.map((item) => (
                    <MenuItem key={item} value={item}>
                      {formatLabel(item)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel id="record-severity-label">Severity</InputLabel>
                <Select
                  labelId="record-severity-label"
                  label="Severity"
                  value={severity}
                  onChange={(event) => setSeverity(event.target.value as ComplianceSeverity)}
                >
                  {recordSeverities.map((item) => (
                    <MenuItem key={item} value={item}>
                      {item}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2,
              }}
            >
              <FormControl fullWidth>
                <InputLabel id="record-field-label">Field (optional)</InputLabel>
                <Select
                  labelId="record-field-label"
                  label="Field (optional)"
                  value={fieldId}
                  onChange={(event) => setFieldId(event.target.value)}
                >
                  <MenuItem value="">Farm-wide</MenuItem>
                  {fields.map((field) => (
                    <MenuItem key={field.id} value={field.id}>
                      {field.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel id="record-worker-label">Worker (optional)</InputLabel>
                <Select
                  labelId="record-worker-label"
                  label="Worker (optional)"
                  value={workerId}
                  onChange={(event) => setWorkerId(event.target.value)}
                >
                  <MenuItem value="">None</MenuItem>
                  {workers.map((worker) => (
                    <MenuItem key={worker.id} value={worker.id}>
                      {worker.fullName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="Checked at"
                type="datetime-local"
                value={checkedAtLocal}
                onChange={(event) => setCheckedAtLocal(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                required
              />
              <TextField
                label="Due date (optional)"
                type="datetime-local"
                value={dueDateLocal}
                onChange={(event) => setDueDateLocal(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Box>

            <TextField
              label="Inspector / reviewer name"
              value={inspectorName}
              onChange={(event) => setInspectorName(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 150 } }}
            />
            <TextField
              label="Findings"
              value={findings}
              onChange={(event) => setFindings(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 2000 } }}
              multiline
              minRows={2}
            />
            <TextField
              label="Corrective action"
              value={correctiveAction}
              onChange={(event) => setCorrectiveAction(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 2000 } }}
              multiline
              minRows={2}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 1 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Saving…' : record ? 'Save changes' : 'Create record'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

interface StatusDialogProps {
  record: ComplianceRecord | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (status: ComplianceRecordStatus, correctiveAction: string) => void
}

function StatusUpdateDialog({ record, saving, error, onClose, onSave }: StatusDialogProps) {
  const [status, setStatus] = useState<ComplianceRecordStatus>('Resolved')
  const [correctiveAction, setCorrectiveAction] = useState('')

  useEffect(() => {
    if (!record) return
    setStatus(
      record.status === 'Resolved'
        ? 'Resolved'
        : ('Resolved' as ComplianceRecordStatus),
    )
    setCorrectiveAction(record.correctiveAction ?? '')
  }, [record])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave(status, correctiveAction)
  }

  return (
    <Dialog open={Boolean(record)} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>Update compliance status</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Typography variant="body2" color="text.secondary">
              {record?.title}
            </Typography>
            <FormControl fullWidth>
              <InputLabel id="quick-status-label">Status</InputLabel>
              <Select
                labelId="quick-status-label"
                label="Status"
                value={status}
                onChange={(event) => setStatus(event.target.value as ComplianceRecordStatus)}
              >
                {recordStatuses.map((item) => (
                  <MenuItem key={item} value={item}>
                    {formatLabel(item)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Corrective action"
              value={correctiveAction}
              onChange={(event) => setCorrectiveAction(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 2000 } }}
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
            {saving ? 'Updating…' : 'Update status'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function CompliancePage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canManage = hasPermission('compliance.manage')

  const [tab, setTab] = useState(0)
  const [statusFilter, setStatusFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [severityFilter, setSeverityFilter] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)

  const [formOpen, setFormOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<ComplianceRecord | null>(null)
  const [formError, setFormError] = useState('')

  const [statusRecord, setStatusRecord] = useState<ComplianceRecord | null>(null)
  const [statusError, setStatusError] = useState('')

  const overviewQuery = useQuery({
    queryKey: ['compliance-overview', selectedFarmId],
    queryFn: () =>
      apiRequest<ComplianceOverview>(`/api/farms/${selectedFarmId}/compliance/overview`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const recordsQuery = useQuery({
    queryKey: ['compliance-records', selectedFarmId, statusFilter, typeFilter, severityFilter, search, page],
    queryFn: () => {
      const params = new URLSearchParams()
      if (statusFilter) params.set('status', statusFilter)
      if (typeFilter) params.set('recordType', typeFilter)
      if (severityFilter) params.set('severity', severityFilter)
      if (search.trim()) params.set('search', search.trim())
      params.set('page', String(page))
      params.set('pageSize', '20')
      return apiRequest<PagedResponse<ComplianceRecord>>(
        `/api/farms/${selectedFarmId}/compliance/records?${params.toString()}`,
      )
    },
    enabled: Boolean(selectedFarmId) && tab === 1,
    retry: false,
  })

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId && canManage),
    retry: false,
  })

  const workersQuery = useQuery({
    queryKey: ['workers', selectedFarmId],
    queryFn: () => apiRequest<Worker[]>(`/api/farms/${selectedFarmId}/workers`),
    enabled: Boolean(selectedFarmId && canManage),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: (payload: RecordPayload) =>
      apiRequest<ComplianceRecord>(
        editingRecord
          ? `/api/farms/${selectedFarmId}/compliance/records/${editingRecord.id}`
          : `/api/farms/${selectedFarmId}/compliance/records`,
        {
          method: editingRecord ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['compliance-overview', selectedFarmId] })
      await queryClient.invalidateQueries({ queryKey: ['compliance-records', selectedFarmId] })
      setFormOpen(false)
      setEditingRecord(null)
    },
    onError: (caught) =>
      setFormError(caught instanceof ApiError ? caught.message : 'Could not save compliance record.'),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status, correctiveAction }: { id: string; status: ComplianceRecordStatus; correctiveAction: string }) =>
      apiRequest<ComplianceRecord>(
        `/api/farms/${selectedFarmId}/compliance/records/${id}/status`,
        {
          method: 'PATCH',
          body: JSON.stringify({ status, correctiveAction: correctiveAction.trim() || null }),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['compliance-overview', selectedFarmId] })
      await queryClient.invalidateQueries({ queryKey: ['compliance-records', selectedFarmId] })
      setStatusRecord(null)
    },
    onError: (caught) =>
      setStatusError(caught instanceof ApiError ? caught.message : 'Could not update record status.'),
  })

  if (!selectedFarmId) {
    return (
      <Alert severity="info">
        Select a farm to monitor safety intervals, applicator certifications, and compliance records.
      </Alert>
    )
  }

  const overview = overviewQuery.data

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Safety and compliance
          </Typography>
          <Typography color="text.secondary">
            {selectedFarm
              ? `Safety intervals, certifications, and inspections for ${selectedFarm.name}.`
              : 'Monitor safety restrictions and compliance records.'}
          </Typography>
        </Box>
        {canManage && (
          <Button
            variant="contained"
            startIcon={<AddRoundedIcon />}
            onClick={() => {
              setEditingRecord(null)
              setFormError('')
              setFormOpen(true)
            }}
          >
            Log compliance record
          </Button>
        )}
      </Stack>

      <Tabs value={tab} onChange={(_, value) => setTab(value)}>
        <Tab label="Safety monitor" />
        <Tab label="Compliance records" />
      </Tabs>

      {tab === 0 && (
        <Stack spacing={3}>
          {overviewQuery.isLoading ? (
            <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
              <CircularProgress />
            </Box>
          ) : overviewQuery.error ? (
            <Alert severity="error">
              {overviewQuery.error instanceof ApiError
                ? overviewQuery.error.message
                : 'Could not load safety overview.'}
            </Alert>
          ) : overview ? (
            <>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' },
                  gap: 2,
                }}
              >
                {[
                  {
                    label: 'Expired / missing certs',
                    value: overview.expiredCertifications,
                    color: overview.expiredCertifications > 0 ? 'error.main' : 'success.main',
                    sub: `${overview.expiringCertificationsWithin30Days} expiring within 30 days`,
                  },
                  {
                    label: 'Active REI holds',
                    value: overview.activeReEntryHolds,
                    color: overview.activeReEntryHolds > 0 ? 'warning.main' : 'text.primary',
                    sub: 'Fields currently restricted for re-entry',
                  },
                  {
                    label: 'Active PHI holds',
                    value: overview.activePreHarvestHolds,
                    color: overview.preHarvestHolds.some((h) => h.conflictsWithExpectedHarvest)
                      ? 'error.main'
                      : 'text.primary',
                    sub: 'Planted crops awaiting safe harvest date',
                  },
                  {
                    label: 'Product registration alerts',
                    value: overview.expiringProductRegistrations,
                    color: overview.expiringProductRegistrations > 0 ? 'warning.main' : 'text.primary',
                    sub: 'Expired or expiring within 60 days',
                  },
                  {
                    label: 'Open compliance records',
                    value: overview.openRecords,
                    color: 'info.main',
                    sub: 'Open or in review',
                  },
                  {
                    label: 'Non-compliant findings',
                    value: overview.nonCompliantRecords,
                    color: overview.nonCompliantRecords > 0 ? 'error.main' : 'success.main',
                    sub: 'Require corrective action',
                  },
                ].map((card) => (
                  <Card key={card.label} variant="outlined">
                    <CardContent>
                      <Typography variant="body2" color="text.secondary">
                        {card.label}
                      </Typography>
                      <Typography variant="h4" sx={{ fontWeight: 800, color: card.color, my: 0.5 }}>
                        {card.value}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {card.sub}
                      </Typography>
                    </CardContent>
                  </Card>
                ))}
              </Box>

              {/* Active Re-Entry Holds */}
              <Card variant="outlined">
                <Box sx={{ p: 2.5, pb: 1.5 }}>
                  <Typography variant="h6" sx={{ fontWeight: 750 }}>
                    Active Re-Entry Interval (REI) holds
                  </Typography>
                </Box>
                {overview.reEntryHolds.length === 0 ? (
                  <Box sx={{ px: 2.5, pb: 2.5 }}>
                    <Typography variant="body2" color="text.secondary">
                      No fields are currently under an active re-entry restriction.
                    </Typography>
                  </Box>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Field</TableCell>
                          <TableCell>Crop</TableCell>
                          <TableCell>Products</TableCell>
                          <TableCell>REI</TableCell>
                          <TableCell>Re-entry allowed after</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {overview.reEntryHolds.map((hold) => (
                          <TableRow key={hold.applicationId} hover>
                            <TableCell sx={{ fontWeight: 600 }}>{hold.fieldName}</TableCell>
                            <TableCell>{hold.cropName}</TableCell>
                            <TableCell>{hold.products.join(', ')}</TableCell>
                            <TableCell>{hold.reEntryIntervalHours} h</TableCell>
                            <TableCell>
                              <Chip
                                size="small"
                                color="warning"
                                label={new Date(hold.reEntryAllowedAtUtc).toLocaleString()}
                              />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </Card>

              {/* Active Pre-Harvest Holds */}
              <Card variant="outlined">
                <Box sx={{ p: 2.5, pb: 1.5 }}>
                  <Typography variant="h6" sx={{ fontWeight: 750 }}>
                    Active Pre-Harvest Interval (PHI) holds
                  </Typography>
                </Box>
                {overview.preHarvestHolds.length === 0 ? (
                  <Box sx={{ px: 2.5, pb: 2.5 }}>
                    <Typography variant="body2" color="text.secondary">
                      No planted crops are currently under a pre-harvest interval hold.
                    </Typography>
                  </Box>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Field</TableCell>
                          <TableCell>Crop</TableCell>
                          <TableCell>Products</TableCell>
                          <TableCell>PHI</TableCell>
                          <TableCell>Earliest safe harvest</TableCell>
                          <TableCell>Expected harvest</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {overview.preHarvestHolds.map((hold) => (
                          <TableRow key={hold.applicationId} hover>
                            <TableCell sx={{ fontWeight: 600 }}>{hold.fieldName}</TableCell>
                            <TableCell>{hold.cropName}</TableCell>
                            <TableCell>{hold.products.join(', ')}</TableCell>
                            <TableCell>{hold.preHarvestIntervalDays} d</TableCell>
                            <TableCell>
                              <Chip
                                size="small"
                                color={hold.conflictsWithExpectedHarvest ? 'error' : 'info'}
                                label={hold.earliestSafeHarvestDate}
                              />
                            </TableCell>
                            <TableCell>
                              {hold.expectedHarvestDate ?? '—'}
                              {hold.conflictsWithExpectedHarvest && (
                                <Chip
                                  size="small"
                                  color="error"
                                  label="Conflict"
                                  sx={{ ml: 1 }}
                                />
                              )}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </Card>

              {/* Applicator Certification Monitoring */}
              <Card variant="outlined">
                <Box sx={{ p: 2.5, pb: 1.5 }}>
                  <Typography variant="h6" sx={{ fontWeight: 750 }}>
                    Applicator certification status
                  </Typography>
                </Box>
                {overview.certifications.length === 0 ? (
                  <Box sx={{ px: 2.5, pb: 2.5 }}>
                    <Typography variant="body2" color="text.secondary">
                      No active applicators found for this farm.
                    </Typography>
                  </Box>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Applicator</TableCell>
                          <TableCell>Certification #</TableCell>
                          <TableCell>Expiry date</TableCell>
                          <TableCell>Status</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {overview.certifications.map((cert) => (
                          <TableRow key={cert.workerId} hover>
                            <TableCell>
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {cert.fullName}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {cert.workerCode}
                              </Typography>
                            </TableCell>
                            <TableCell>{cert.certificationNumber ?? '—'}</TableCell>
                            <TableCell>{cert.certificationExpiryDate ?? '—'}</TableCell>
                            <TableCell>
                              <Chip
                                size="small"
                                color={statusChipColor(cert.status)}
                                label={
                                  cert.daysRemaining != null && cert.status === 'ExpiringSoon'
                                    ? `Expiring (${cert.daysRemaining}d)`
                                    : formatLabel(cert.status)
                                }
                              />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </Card>

              {/* Product Registration Alerts */}
              {overview.productRegistrations.length > 0 && (
                <Card variant="outlined">
                  <Box sx={{ p: 2.5, pb: 1.5 }}>
                    <Typography variant="h6" sx={{ fontWeight: 750 }}>
                      Product registration alerts
                    </Typography>
                  </Box>
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Product</TableCell>
                          <TableCell>Registration #</TableCell>
                          <TableCell>Expiry date</TableCell>
                          <TableCell>Status</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {overview.productRegistrations.map((reg) => (
                          <TableRow key={reg.productId} hover>
                            <TableCell sx={{ fontWeight: 600 }}>{reg.productName}</TableCell>
                            <TableCell>{reg.registrationNumber ?? '—'}</TableCell>
                            <TableCell>{reg.registrationExpiryDate}</TableCell>
                            <TableCell>
                              <Chip
                                size="small"
                                color={statusChipColor(reg.status)}
                                label={
                                  reg.status === 'Expired'
                                    ? 'Expired'
                                    : `Expires in ${reg.daysRemaining}d`
                                }
                              />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Card>
              )}
            </>
          ) : null}
        </Stack>
      )}

      {tab === 1 && (
        <Stack spacing={2.5}>
          <Card variant="outlined">
            <Box
              sx={{
                p: 2.5,
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '2fr 1fr 1fr 1fr' },
                gap: 2,
              }}
            >
              <TextField
                size="small"
                label="Search"
                placeholder="Title, inspector, findings…"
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value)
                  setPage(1)
                }}
              />
              <FormControl size="small">
                <InputLabel id="filter-status-label">Status</InputLabel>
                <Select
                  labelId="filter-status-label"
                  label="Status"
                  value={statusFilter}
                  onChange={(event) => {
                    setStatusFilter(event.target.value)
                    setPage(1)
                  }}
                >
                  <MenuItem value="">All</MenuItem>
                  {recordStatuses.map((item) => (
                    <MenuItem key={item} value={item}>
                      {formatLabel(item)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small">
                <InputLabel id="filter-type-label">Type</InputLabel>
                <Select
                  labelId="filter-type-label"
                  label="Type"
                  value={typeFilter}
                  onChange={(event) => {
                    setTypeFilter(event.target.value)
                    setPage(1)
                  }}
                >
                  <MenuItem value="">All</MenuItem>
                  {recordTypes.map((item) => (
                    <MenuItem key={item} value={item}>
                      {formatLabel(item)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small">
                <InputLabel id="filter-severity-label">Severity</InputLabel>
                <Select
                  labelId="filter-severity-label"
                  label="Severity"
                  value={severityFilter}
                  onChange={(event) => {
                    setSeverityFilter(event.target.value)
                    setPage(1)
                  }}
                >
                  <MenuItem value="">All</MenuItem>
                  {recordSeverities.map((item) => (
                    <MenuItem key={item} value={item}>
                      {item}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>

            {recordsQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : recordsQuery.error ? (
              <Box sx={{ p: 2.5 }}>
                <Alert severity="error">
                  {recordsQuery.error instanceof ApiError
                    ? recordsQuery.error.message
                    : 'Could not load compliance records.'}
                </Alert>
              </Box>
            ) : !recordsQuery.data?.items.length ? (
              <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
                <HealthAndSafetyRoundedIcon color="primary" sx={{ fontSize: 48 }} />
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  No compliance records found
                </Typography>
                <Typography color="text.secondary">
                  Log inspections, regulatory checks, or safety incidents to track corrective actions.
                </Typography>
              </Stack>
            ) : (
              <>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Record</TableCell>
                        <TableCell>Scope</TableCell>
                        <TableCell>Severity</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Checked at</TableCell>
                        {canManage && <TableCell align="right">Actions</TableCell>}
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {recordsQuery.data.items.map((record) => (
                        <TableRow key={record.id} hover>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontWeight: 700 }}>
                              {record.title}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {formatLabel(record.recordType)}
                              {record.inspectorName ? ` · ${record.inspectorName}` : ''}
                            </Typography>
                            {record.findings && (
                              <Typography
                                variant="caption"
                                color="text.secondary"
                                sx={{ display: 'block', mt: 0.5 }}
                              >
                                {record.findings}
                              </Typography>
                            )}
                          </TableCell>
                          <TableCell>
                            {[record.fieldName, record.workerName].filter(Boolean).join(' · ') || 'Farm-wide'}
                          </TableCell>
                          <TableCell>
                            <Chip
                              size="small"
                              label={record.severity}
                              color={severityChipColor(record.severity)}
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>
                            <Chip
                              size="small"
                              label={formatLabel(record.status)}
                              color={statusChipColor(record.status)}
                            />
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2">
                              {new Date(record.checkedAtUtc).toLocaleDateString()}
                            </Typography>
                            {record.dueDateUtc && record.status !== 'Resolved' && (
                              <Typography variant="caption" color="text.secondary">
                                Due {new Date(record.dueDateUtc).toLocaleDateString()}
                              </Typography>
                            )}
                          </TableCell>
                          {canManage && (
                            <TableCell align="right">
                              <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
                                <Tooltip title="Update status">
                                  <IconButton
                                    aria-label={`Update status for ${record.title}`}
                                    onClick={() => {
                                      setStatusError('')
                                      setStatusRecord(record)
                                    }}
                                  >
                                    <CheckCircleOutlineRoundedIcon />
                                  </IconButton>
                                </Tooltip>
                                <Tooltip title="Edit record">
                                  <IconButton
                                    aria-label={`Edit ${record.title}`}
                                    onClick={() => {
                                      setEditingRecord(record)
                                      setFormError('')
                                      setFormOpen(true)
                                    }}
                                  >
                                    <EditRoundedIcon />
                                  </IconButton>
                                </Tooltip>
                              </Stack>
                            </TableCell>
                          )}
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
                {recordsQuery.data.totalPages > 1 && (
                  <Box sx={{ p: 2, display: 'flex', justifyContent: 'center' }}>
                    <Pagination
                      count={recordsQuery.data.totalPages}
                      page={page}
                      onChange={(_, nextPage) => setPage(nextPage)}
                      color="primary"
                    />
                  </Box>
                )}
              </>
            )}
          </Card>
        </Stack>
      )}

      <RecordFormDialog
        open={formOpen}
        record={editingRecord}
        fields={fieldsQuery.data ?? []}
        workers={workersQuery.data ?? []}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setFormOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />

      <StatusUpdateDialog
        record={statusRecord}
        saving={statusMutation.isPending}
        error={statusError}
        onClose={() => setStatusRecord(null)}
        onSave={(status, correctiveAction) => {
          if (statusRecord) {
            statusMutation.mutate({ id: statusRecord.id, status, correctiveAction })
          }
        }}
      />
    </Stack>
  )
}
