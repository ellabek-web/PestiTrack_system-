import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  MenuItem,
  Pagination,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded'
import { useAuth } from '../auth/AuthContext'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { AuditLog, PagedResponse } from '../types/api'

function formatAuditValue(value: unknown) {
  if (value == null) return '—'
  if (typeof value === 'string') return value
  return JSON.stringify(value)
}

export function AuditPage() {
  const { isAdministrator, isAuditor } = useAuth()
  const { farms, selectedFarmId, selectedFarm } = useFarm()
  const systemAccess = isAdministrator || isAuditor
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [action, setAction] = useState('')
  const [entityType, setEntityType] = useState('')
  const [farmFilter, setFarmFilter] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [selected, setSelected] = useState<AuditLog | null>(null)
  const params = new URLSearchParams({ page: String(page), pageSize: '25' })
  if (search.trim()) params.set('search', search.trim())
  if (action) params.set('action', action)
  if (entityType.trim()) params.set('entityType', entityType.trim())
  if (systemAccess && farmFilter) params.set('farmId', farmFilter)
  if (from) params.set('fromUtc', new Date(`${from}T00:00:00`).toISOString())
  if (to) params.set('toUtc', new Date(`${to}T23:59:59`).toISOString())
  const basePath = systemAccess
    ? '/api/admin/audit-logs'
    : `/api/farms/${selectedFarmId}/audit-logs`

  const hasActiveFilters = Boolean(
    search || action || entityType || (systemAccess && farmFilter) || from || to,
  )

  const clearFilters = () => {
    setSearch('')
    setAction('')
    setEntityType('')
    setFarmFilter('')
    setFrom('')
    setTo('')
    setPage(1)
  }

  const auditQuery = useQuery({
    queryKey: [
      'audit-logs',
      basePath,
      page,
      search,
      action,
      entityType,
      systemAccess ? farmFilter : selectedFarmId,
      from,
      to,
    ],
    queryFn: () => apiRequest<PagedResponse<AuditLog>>(`${basePath}?${params}`),
    enabled: systemAccess || Boolean(selectedFarmId),
    retry: false,
  })

  if (!systemAccess && !selectedFarmId) {
    return <Alert severity="info">Select a farm to review its audit trail.</Alert>
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>Audit trail</Typography>
        <Typography color="text.secondary">
          {systemAccess
            ? 'Review administrative and operational changes across all farms.'
            : `Review recorded changes for ${selectedFarm?.name}.`}
        </Typography>
      </Box>

      <Card variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '2fr 1fr 1fr' }, gap: 2 }}>
            <TextField
              label="Search"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Actor, entity ID or request path"
            />
            <TextField
              select
              label="Action"
              value={action}
              onChange={(event) => {
                setAction(event.target.value)
                setPage(1)
              }}
            >
              <MenuItem value="">All actions</MenuItem>
              <MenuItem value="Create">Create</MenuItem>
              <MenuItem value="Update">Update</MenuItem>
              <MenuItem value="Delete">Delete</MenuItem>
            </TextField>
            <TextField
              label="Entity type"
              value={entityType}
              onChange={(event) => {
                setEntityType(event.target.value)
                setPage(1)
              }}
              placeholder="Farm, Worker, ApplicationUser…"
            />
          </Box>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                md: systemAccess ? '1fr 1fr 1fr auto' : '1fr 1fr auto',
              },
              gap: 2,
              alignItems: 'center',
            }}
          >
            <TextField
              label="From"
              type="date"
              value={from}
              onChange={(event) => {
                setFrom(event.target.value)
                setPage(1)
              }}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label="To"
              type="date"
              value={to}
              onChange={(event) => {
                setTo(event.target.value)
                setPage(1)
              }}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            {systemAccess && (
              <TextField
                select
                label="Farm scope"
                value={farmFilter}
                onChange={(event) => {
                  setFarmFilter(event.target.value)
                  setPage(1)
                }}
              >
                <MenuItem value="">All farms &amp; system</MenuItem>
                {farms.map((farm) => (
                  <MenuItem key={farm.id} value={farm.id}>
                    {farm.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
            <Button onClick={clearFilters} disabled={!hasActiveFilters}>
              Clear filters
            </Button>
          </Box>
        </Stack>
      </Card>

      {auditQuery.error && (
        <Alert severity={auditQuery.error instanceof ApiError && auditQuery.error.status === 403 ? 'warning' : 'error'}>
          {auditQuery.error instanceof ApiError
            ? auditQuery.error.message
            : 'Could not load audit records.'}
        </Alert>
      )}

      <Card variant="outlined">
        {auditQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>
        ) : !auditQuery.data?.items.length ? (
          <Alert severity="info" sx={{ m: 2 }}>No audit records match these filters.</Alert>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow><TableCell>Time</TableCell><TableCell>Actor</TableCell><TableCell>Action</TableCell><TableCell>Entity</TableCell><TableCell>Request</TableCell><TableCell align="right">Changes</TableCell></TableRow>
                </TableHead>
                <TableBody>
                  {auditQuery.data.items.map((log) => (
                    <TableRow key={log.id} hover>
                      <TableCell>{new Date(log.occurredAtUtc).toLocaleString()}</TableCell>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>{log.actorDisplayName ?? 'System'}</Typography>
                        <Typography variant="caption" color="text.secondary">{log.actorEmail}</Typography>
                      </TableCell>
                      <TableCell><Chip size="small" label={log.action} color={log.action === 'Delete' ? 'error' : log.action === 'Create' ? 'success' : 'default'} /></TableCell>
                      <TableCell>
                        <Typography variant="body2">{log.entityType}</Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>{log.entityId}</Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{log.httpMethod ?? '—'}</Typography>
                        <Typography variant="caption" color="text.secondary">{log.requestPath ?? 'Background operation'}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" startIcon={<VisibilityRoundedIcon />} onClick={() => setSelected(log)}>View</Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            <Stack direction="row" sx={{ p: 2, justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="body2" color="text.secondary">{auditQuery.data.totalCount} records</Typography>
              <Pagination count={auditQuery.data.totalPages} page={page} onChange={(_, value) => setPage(value)} />
            </Stack>
          </>
        )}
      </Card>

      <Dialog open={Boolean(selected)} onClose={() => setSelected(null)} fullWidth maxWidth="md">
        <DialogTitle>{selected?.action} {selected?.entityType}</DialogTitle>
        <DialogContent>
          {selected && (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Alert severity="info">
                {new Date(selected.occurredAtUtc).toLocaleString()} · {selected.actorDisplayName ?? selected.actorEmail ?? 'System'} · {selected.ipAddress ?? 'No IP'}
              </Alert>
              {!Object.keys(selected.changes).length ? (
                <Typography color="text.secondary">No property-level changes were recorded.</Typography>
              ) : (
                <Table size="small">
                  <TableHead><TableRow><TableCell>Property</TableCell><TableCell>Previous</TableCell><TableCell>New</TableCell></TableRow></TableHead>
                  <TableBody>
                    {Object.entries(selected.changes).map(([property, change]) => (
                      <TableRow key={property}>
                        <TableCell sx={{ fontWeight: 700 }}>{property}</TableCell>
                        <TableCell sx={{ wordBreak: 'break-word' }}>{formatAuditValue(change.oldValue)}</TableCell>
                        <TableCell sx={{ wordBreak: 'break-word' }}>{formatAuditValue(change.newValue)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </Stack>
          )}
        </DialogContent>
      </Dialog>
    </Stack>
  )
}
