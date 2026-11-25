import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Pagination,
  Select,
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
import { ApplicationDetailDialog } from '../components/ApplicationDetailDialog'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type {
  ApplicationHistoryItem,
  Field,
  PagedResponse,
  Product,
  Worker,
} from '../types/api'

export function ApplicationHistoryPage() {
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const [searchParams, setSearchParams] = useSearchParams()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [fieldId, setFieldId] = useState('')
  const [workerId, setWorkerId] = useState('')
  const [productId, setProductId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')

  const selectedId = searchParams.get('detail')
  const openDetail = (id: string | null) => {
    const next = new URLSearchParams(searchParams)
    if (id) next.set('detail', id)
    else next.delete('detail')
    setSearchParams(next, { replace: true })
  }

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId) && hasPermission('fields.view'),
    retry: false,
  })
  const workersQuery = useQuery({
    queryKey: ['workers', selectedFarmId],
    queryFn: () => apiRequest<Worker[]>(`/api/farms/${selectedFarmId}/workers`),
    enabled: Boolean(selectedFarmId) && hasPermission('workers.view'),
    retry: false,
  })
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => apiRequest<Product[]>('/api/products'),
    retry: false,
  })

  const params = new URLSearchParams({ page: String(page), pageSize: '20' })
  if (search.trim()) params.set('search', search.trim())
  if (status) params.set('status', status)
  if (fieldId) params.set('fieldId', fieldId)
  if (workerId) params.set('workerId', workerId)
  if (productId) params.set('productId', productId)
  if (from) params.set('fromUtc', new Date(`${from}T00:00:00`).toISOString())
  if (to) params.set('toUtc', new Date(`${to}T23:59:59`).toISOString())

  const hasActiveFilters = Boolean(
    search || status || fieldId || workerId || productId || from || to,
  )

  const clearFilters = () => {
    setSearch('')
    setStatus('')
    setFieldId('')
    setWorkerId('')
    setProductId('')
    setFrom('')
    setTo('')
    setPage(1)
  }

  const historyQuery = useQuery({
    queryKey: [
      'application-history',
      selectedFarmId,
      page,
      search,
      status,
      fieldId,
      workerId,
      productId,
      from,
      to,
    ],
    queryFn: () =>
      apiRequest<PagedResponse<ApplicationHistoryItem>>(
        `/api/farms/${selectedFarmId}/applications/history?${params}`,
      ),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to review application history.</Alert>
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
          Application history
        </Typography>
        <Typography color="text.secondary">
          Search completed and draft treatment records for {selectedFarm?.name}.
        </Typography>
      </Box>

      <Card variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: '2fr 1fr 1fr 1fr' },
              gap: 2,
            }}
          >
            <TextField
              label="Search"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Pest, field, crop, worker, product or batch"
            />
            <FormControl>
              <InputLabel id="history-status-label">Status</InputLabel>
              <Select
                labelId="history-status-label"
                label="Status"
                value={status}
                onChange={(event) => {
                  setStatus(event.target.value)
                  setPage(1)
                }}
              >
                <MenuItem value="">All statuses</MenuItem>
                <MenuItem value="Draft">Draft</MenuItem>
                <MenuItem value="Completed">Completed</MenuItem>
              </Select>
            </FormControl>
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
          </Box>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: '1fr 1fr 1fr auto' },
              gap: 2,
              alignItems: 'center',
            }}
          >
            <TextField
              select
              label="Field"
              value={fieldId}
              onChange={(event) => {
                setFieldId(event.target.value)
                setPage(1)
              }}
              disabled={!fieldsQuery.data?.length}
            >
              <MenuItem value="">All fields</MenuItem>
              {(fieldsQuery.data ?? []).map((field) => (
                <MenuItem key={field.id} value={field.id}>
                  {field.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="Applicator"
              value={workerId}
              onChange={(event) => {
                setWorkerId(event.target.value)
                setPage(1)
              }}
              disabled={!workersQuery.data?.length}
            >
              <MenuItem value="">All applicators</MenuItem>
              {(workersQuery.data ?? []).map((worker) => (
                <MenuItem key={worker.id} value={worker.id}>
                  {worker.fullName}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="Product"
              value={productId}
              onChange={(event) => {
                setProductId(event.target.value)
                setPage(1)
              }}
              disabled={!productsQuery.data?.length}
            >
              <MenuItem value="">All products</MenuItem>
              {(productsQuery.data ?? []).map((product) => (
                <MenuItem key={product.id} value={product.id}>
                  {product.name}
                </MenuItem>
              ))}
            </TextField>
            <Button onClick={clearFilters} disabled={!hasActiveFilters}>
              Clear filters
            </Button>
          </Box>
        </Stack>
      </Card>

      {historyQuery.error && (
        <Alert
          severity={
            historyQuery.error instanceof ApiError && historyQuery.error.status === 403
              ? 'warning'
              : 'error'
          }
        >
          {historyQuery.error instanceof ApiError && historyQuery.error.status === 403
            ? 'You do not have permission to view application history for this farm.'
            : historyQuery.error instanceof ApiError
              ? historyQuery.error.message
              : 'Could not load application history.'}
        </Alert>
      )}

      <Card variant="outlined">
        {historyQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !historyQuery.data?.items.length ? (
          <Alert severity="info" sx={{ m: 2 }}>
            No application records match these filters.
          </Alert>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Date</TableCell>
                    <TableCell>Field / crop</TableCell>
                    <TableCell>Target / applicator</TableCell>
                    <TableCell>Products</TableCell>
                    <TableCell>Traceability</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell align="right">Details</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {historyQuery.data.items.map((item) => (
                    <TableRow key={item.id} hover>
                      <TableCell>{new Date(item.applicationDateUtc).toLocaleString()}</TableCell>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {item.fieldName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {item.cropName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{item.targetPest}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {item.workerName}
                        </Typography>
                      </TableCell>
                      <TableCell>{item.productCount}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={item.hasInventoryTraceability ? 'Linked' : 'Pending'}
                          color={item.hasInventoryTraceability ? 'success' : 'default'}
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={item.status}
                          color={item.status === 'Completed' ? 'success' : 'warning'}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          startIcon={<VisibilityRoundedIcon />}
                          onClick={() => openDetail(item.id)}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            <Stack
              direction="row"
              sx={{ p: 2, justifyContent: 'space-between', alignItems: 'center' }}
            >
              <Typography variant="body2" color="text.secondary">
                {historyQuery.data.totalCount} records
              </Typography>
              <Pagination
                page={page}
                count={historyQuery.data.totalPages}
                onChange={(_, value) => setPage(value)}
              />
            </Stack>
          </>
        )}
      </Card>

      <ApplicationDetailDialog
        farmId={selectedFarmId}
        applicationId={selectedId}
        onClose={() => openDetail(null)}
      />
    </Stack>
  )
}
