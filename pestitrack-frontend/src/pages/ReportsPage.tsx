import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  CircularProgress,
  MenuItem,
  Pagination,
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
  Typography,
} from '@mui/material'
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiDownload, apiRequest } from '../lib/api'
import type {
  ApplicationReport,
  Field,
  InventoryReport,
  Product,
  ProductUsageReport,
  Worker,
  WorkerActivityReport,
} from '../types/api'

const reportSlugs = ['applications', 'inventory', 'product-usage', 'worker-activity'] as const
type ReportSlug = (typeof reportSlugs)[number]

export function ReportsPage() {
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canExport = hasPermission('reports.export')
  const [tab, setTab] = useState(0)
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [fieldId, setFieldId] = useState('')
  const [workerId, setWorkerId] = useState('')
  const [productId, setProductId] = useState('')
  const [status, setStatus] = useState('')
  const [transactionType, setTransactionType] = useState('')
  const [batchNumber, setBatchNumber] = useState('')
  const [page, setPage] = useState(1)
  const [format, setFormat] = useState('xlsx')
  const [downloadError, setDownloadError] = useState('')
  const slug: ReportSlug = reportSlugs[tab]
  const isInventory = slug === 'inventory'

  const fieldsQuery = useQuery({
    queryKey: ['fields', selectedFarmId],
    queryFn: () => apiRequest<Field[]>(`/api/farms/${selectedFarmId}/fields`),
    enabled: Boolean(selectedFarmId) && !isInventory && hasPermission('fields.view'),
    retry: false,
  })
  const workersQuery = useQuery({
    queryKey: ['workers', selectedFarmId],
    queryFn: () => apiRequest<Worker[]>(`/api/farms/${selectedFarmId}/workers`),
    enabled: Boolean(selectedFarmId) && !isInventory && hasPermission('workers.view'),
    retry: false,
  })
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => apiRequest<Product[]>('/api/products'),
    retry: false,
  })

  const query = new URLSearchParams({ page: String(page), pageSize: '50' })
  if (from) query.set('fromUtc', new Date(`${from}T00:00:00`).toISOString())
  if (to) query.set('toUtc', new Date(`${to}T23:59:59`).toISOString())
  if (productId) query.set('productId', productId)
  if (isInventory) {
    if (transactionType) query.set('transactionType', transactionType)
    if (batchNumber.trim()) query.set('batchNumber', batchNumber.trim())
  } else {
    if (fieldId) query.set('fieldId', fieldId)
    if (workerId) query.set('workerId', workerId)
    if (status && slug !== 'product-usage') query.set('status', status)
  }

  const hasActiveFilters = Boolean(
    from ||
      to ||
      productId ||
      (isInventory ? transactionType || batchNumber : fieldId || workerId || status),
  )

  const clearFilters = () => {
    setFrom('')
    setTo('')
    setFieldId('')
    setWorkerId('')
    setProductId('')
    setStatus('')
    setTransactionType('')
    setBatchNumber('')
    setPage(1)
  }

  const reportQuery = useQuery({
    queryKey: [
      'report',
      selectedFarmId,
      slug,
      from,
      to,
      fieldId,
      workerId,
      productId,
      status,
      transactionType,
      batchNumber,
      page,
    ],
    queryFn: () =>
      apiRequest<ApplicationReport | InventoryReport | ProductUsageReport | WorkerActivityReport>(
        `/api/farms/${selectedFarmId}/reports/${slug}?${query}`,
      ),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const download = async () => {
    setDownloadError('')
    try {
      await apiDownload(
        `/api/farms/${selectedFarmId}/reports/${slug}/export/${format}?${query}`,
        `${slug}-report.${format}`,
      )
    } catch (error) {
      setDownloadError(error instanceof ApiError ? error.message : 'Could not export the report.')
    }
  }

  if (!selectedFarmId) return <Alert severity="info">Select a farm to view reports.</Alert>

  const data = reportQuery.data
  const paged = data && 'page' in data ? data : null

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
          Reports
        </Typography>
        <Typography color="text.secondary">
          Analyze operations and export records for {selectedFarm?.name}.
        </Typography>
      </Box>

      <Card variant="outlined">
        <Tabs
          value={tab}
          onChange={(_, value: number) => {
            setTab(value)
            setPage(1)
          }}
          variant="scrollable"
          sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
        >
          <Tab label="Applications" />
          <Tab label="Inventory" />
          <Tab label="Product usage" />
          <Tab label="Worker activity" />
        </Tabs>

        <Stack spacing={2} sx={{ p: 2.5 }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: 'repeat(4, 1fr)' },
              gap: 2,
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

            {isInventory ? (
              <TextField
                select
                label="Transaction type"
                value={transactionType}
                onChange={(event) => {
                  setTransactionType(event.target.value)
                  setPage(1)
                }}
              >
                <MenuItem value="">All types</MenuItem>
                <MenuItem value="StockIn">Stock in</MenuItem>
                <MenuItem value="StockOut">Stock out</MenuItem>
                <MenuItem value="AdjustmentIn">Adjustment in</MenuItem>
                <MenuItem value="AdjustmentOut">Adjustment out</MenuItem>
              </TextField>
            ) : (
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
            )}
          </Box>

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            sx={{ alignItems: { md: 'center' } }}
          >
            {isInventory ? (
              <TextField
                label="Batch number"
                value={batchNumber}
                onChange={(event) => {
                  setBatchNumber(event.target.value)
                  setPage(1)
                }}
                sx={{ minWidth: { md: 220 } }}
              />
            ) : (
              <>
                <TextField
                  select
                  label="Worker"
                  value={workerId}
                  onChange={(event) => {
                    setWorkerId(event.target.value)
                    setPage(1)
                  }}
                  disabled={!workersQuery.data?.length}
                  sx={{ minWidth: { md: 220 } }}
                >
                  <MenuItem value="">All workers</MenuItem>
                  {(workersQuery.data ?? []).map((worker) => (
                    <MenuItem key={worker.id} value={worker.id}>
                      {worker.fullName}
                    </MenuItem>
                  ))}
                </TextField>
                {slug !== 'product-usage' && (
                  <TextField
                    select
                    label="Status"
                    value={status}
                    onChange={(event) => {
                      setStatus(event.target.value)
                      setPage(1)
                    }}
                    sx={{ minWidth: { md: 160 } }}
                  >
                    <MenuItem value="">All statuses</MenuItem>
                    <MenuItem value="Draft">Draft</MenuItem>
                    <MenuItem value="Completed">Completed</MenuItem>
                  </TextField>
                )}
              </>
            )}

            <Button onClick={clearFilters} disabled={!hasActiveFilters}>
              Clear filters
            </Button>

            <Box sx={{ flex: 1 }} />

            {canExport && (
              <>
                <TextField
                  select
                  label="Format"
                  value={format}
                  onChange={(event) => setFormat(event.target.value)}
                  sx={{ minWidth: 120 }}
                >
                  <MenuItem value="csv">CSV</MenuItem>
                  <MenuItem value="xlsx">Excel</MenuItem>
                  <MenuItem value="pdf">PDF</MenuItem>
                </TextField>
                <Button
                  variant="contained"
                  startIcon={<DownloadRoundedIcon />}
                  onClick={() => void download()}
                >
                  Export
                </Button>
              </>
            )}
          </Stack>
        </Stack>
      </Card>

      {(downloadError || reportQuery.error) && (
        <Alert
          severity={
            !downloadError && reportQuery.error instanceof ApiError && reportQuery.error.status === 403
              ? 'warning'
              : 'error'
          }
        >
          {downloadError ||
            (reportQuery.error instanceof ApiError && reportQuery.error.status === 403
              ? 'You do not have permission to view reports for this farm.'
              : reportQuery.error instanceof ApiError
                ? reportQuery.error.message
                : 'Could not load the report.')}
        </Alert>
      )}

      <Card variant="outlined">
        {reportQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !data || !data.items.length ? (
          <Alert severity="info" sx={{ m: 2 }}>
            No report data matches these filters.
          </Alert>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  {slug === 'applications' && (
                    <TableRow>
                      <TableCell>Date</TableCell>
                      <TableCell>Field / crop</TableCell>
                      <TableCell>Worker</TableCell>
                      <TableCell>Target</TableCell>
                      <TableCell>Area</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  )}
                  {slug === 'inventory' && (
                    <TableRow>
                      <TableCell>Date</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Quantity</TableCell>
                      <TableCell>Batch</TableCell>
                      <TableCell>Reference</TableCell>
                    </TableRow>
                  )}
                  {slug === 'product-usage' && (
                    <TableRow>
                      <TableCell>Product</TableCell>
                      <TableCell>Applications</TableCell>
                      <TableCell>Total used</TableCell>
                      <TableCell>First use</TableCell>
                      <TableCell>Last use</TableCell>
                    </TableRow>
                  )}
                  {slug === 'worker-activity' && (
                    <TableRow>
                      <TableCell>Worker</TableCell>
                      <TableCell>Applications</TableCell>
                      <TableCell>Completed</TableCell>
                      <TableCell>Area treated</TableCell>
                      <TableCell>Last application</TableCell>
                    </TableRow>
                  )}
                </TableHead>
                <TableBody>
                  {slug === 'applications' &&
                    (data as ApplicationReport).items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>
                          {new Date(item.applicationDateUtc).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          {item.fieldName} / {item.cropName}
                        </TableCell>
                        <TableCell>{item.workerName}</TableCell>
                        <TableCell>{item.targetPest}</TableCell>
                        <TableCell>{item.areaTreatedHectares} ha</TableCell>
                        <TableCell>{item.status}</TableCell>
                      </TableRow>
                    ))}
                  {slug === 'inventory' &&
                    (data as InventoryReport).items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{new Date(item.createdAtUtc).toLocaleDateString()}</TableCell>
                        <TableCell>{item.productName}</TableCell>
                        <TableCell>{item.transactionType}</TableCell>
                        <TableCell>{item.quantity}</TableCell>
                        <TableCell>{item.batchNumber ?? '—'}</TableCell>
                        <TableCell>{item.reference ?? '—'}</TableCell>
                      </TableRow>
                    ))}
                  {slug === 'product-usage' &&
                    (data as ProductUsageReport).items.map((item) => (
                      <TableRow key={item.productId}>
                        <TableCell>{item.productName}</TableCell>
                        <TableCell>{item.applicationCount}</TableCell>
                        <TableCell>{item.totalQuantityUsed}</TableCell>
                        <TableCell>
                          {item.firstApplicationDateUtc
                            ? new Date(item.firstApplicationDateUtc).toLocaleDateString()
                            : '—'}
                        </TableCell>
                        <TableCell>
                          {item.lastApplicationDateUtc
                            ? new Date(item.lastApplicationDateUtc).toLocaleDateString()
                            : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  {slug === 'worker-activity' &&
                    (data as WorkerActivityReport).items.map((item) => (
                      <TableRow key={item.workerId}>
                        <TableCell>
                          {item.workerName} ({item.workerCode})
                        </TableCell>
                        <TableCell>{item.applicationCount}</TableCell>
                        <TableCell>{item.completedApplicationCount}</TableCell>
                        <TableCell>{item.totalAreaTreatedHectares} ha</TableCell>
                        <TableCell>
                          {item.lastApplicationDateUtc
                            ? new Date(item.lastApplicationDateUtc).toLocaleDateString()
                            : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </TableContainer>
            {paged && (
              <Stack
                direction="row"
                sx={{ p: 2, justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Typography variant="body2" color="text.secondary">
                  {paged.totalCount} records
                </Typography>
                <Pagination
                  count={paged.totalPages}
                  page={page}
                  onChange={(_, value) => setPage(value)}
                />
              </Stack>
            )}
          </>
        )}
      </Card>
    </Stack>
  )
}
