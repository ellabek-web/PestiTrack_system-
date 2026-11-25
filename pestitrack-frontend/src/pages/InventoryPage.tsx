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
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import RemoveRoundedIcon from '@mui/icons-material/RemoveRounded'
import TuneRoundedIcon from '@mui/icons-material/TuneRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type {
  InventoryBalance,
  InventoryThreshold,
  InventoryTransaction,
  InventoryTransactionType,
  PagedResponse,
  Product,
} from '../types/api'

/* oxlint-disable react/set-state-in-effect -- transaction form resets from the chosen balance */

type Operation = 'receive' | 'issue' | 'adjustment'

interface InventoryPayload {
  productId: string
  quantity: number
  batchNumber: string
  expiryDate?: string | null
  direction?: number
  reason?: string
  reference: string | null
  notes: string | null
}

interface InventoryFormProps {
  open: boolean
  operation: Operation
  balance: InventoryBalance | null
  products: Product[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: InventoryPayload) => void
}

function operationTitle(operation: Operation) {
  if (operation === 'receive') return 'Receive stock'
  if (operation === 'issue') return 'Issue stock'
  return 'Adjust stock'
}

function InventoryForm({
  open,
  operation,
  balance,
  products,
  saving,
  error,
  onClose,
  onSave,
}: InventoryFormProps) {
  const [productId, setProductId] = useState('')
  const [quantity, setQuantity] = useState('')
  const [batchNumber, setBatchNumber] = useState('')
  const [expiryDate, setExpiryDate] = useState('')
  const [direction, setDirection] = useState(1)
  const [reason, setReason] = useState('')
  const [reference, setReference] = useState('')
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (!open) return
    setProductId(balance?.productId ?? products[0]?.id ?? '')
    setQuantity('')
    setBatchNumber(balance?.batchNumber ?? '')
    setExpiryDate(balance?.expiryDate ?? '')
    setDirection(1)
    setReason('')
    setReference('')
    setNotes('')
  }, [open, balance, products])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      productId,
      quantity: Number(quantity),
      batchNumber: batchNumber.trim(),
      ...(operation !== 'issue' ? { expiryDate: expiryDate || null } : {}),
      ...(operation === 'adjustment'
        ? { direction, reason: reason.trim() }
        : {}),
      reference: reference.trim() || null,
      notes: notes.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{operationTitle(operation)}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            {operation === 'issue' && balance && (
              <Alert severity="info">Available in this batch: {balance.quantityOnHand}</Alert>
            )}
            <FormControl required fullWidth>
              <InputLabel id="inventory-product-label">Product</InputLabel>
              <Select
                labelId="inventory-product-label"
                label="Product"
                value={productId}
                onChange={(event) => setProductId(event.target.value)}
                disabled={Boolean(balance)}
              >
                {products.map((product) => (
                  <MenuItem key={product.id} value={product.id}>
                    {product.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2.5,
              }}
            >
              <TextField
                label="Quantity"
                type="number"
                value={quantity}
                onChange={(event) => setQuantity(event.target.value)}
                slotProps={{ htmlInput: { min: 0.001, step: 0.001 } }}
                required
              />
              <TextField
                label="Batch number"
                value={batchNumber}
                onChange={(event) => setBatchNumber(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
                disabled={Boolean(balance)}
                required
              />
            </Box>
            {operation !== 'issue' && (
              <TextField
                label="Expiry date"
                type="date"
                value={expiryDate}
                onChange={(event) => setExpiryDate(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            )}
            {operation === 'adjustment' && (
              <>
                <FormControl required fullWidth>
                  <InputLabel id="direction-label">Direction</InputLabel>
                  <Select
                    labelId="direction-label"
                    label="Direction"
                    value={direction}
                    onChange={(event) => setDirection(Number(event.target.value))}
                  >
                    <MenuItem value={1}>Increase stock</MenuItem>
                    <MenuItem value={2}>Decrease stock</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  label="Adjustment reason"
                  value={reason}
                  onChange={(event) => setReason(event.target.value)}
                  slotProps={{ htmlInput: { maxLength: 500 } }}
                  required
                />
              </>
            )}
            <TextField
              label="Reference"
              value={reference}
              onChange={(event) => setReference(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 100 } }}
              helperText="Optional invoice, delivery, or internal reference"
            />
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
          <Button type="submit" variant="contained" disabled={saving || !products.length}>
            {saving ? 'Saving…' : operationTitle(operation)}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

const transactionTypeValues: Record<InventoryTransactionType, number> = {
  StockIn: 1,
  StockOut: 2,
  AdjustmentIn: 3,
  AdjustmentOut: 4,
}

function transactionColor(
  type: InventoryTransactionType,
): 'success' | 'warning' | 'info' | 'default' {
  if (type === 'StockIn') return 'success'
  if (type === 'StockOut') return 'warning'
  if (type === 'AdjustmentIn') return 'info'
  return 'default'
}

export function InventoryPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm, hasPermission } = useFarm()
  const canManage = hasPermission('inventory.manage')
  const canIssue = hasPermission('inventory.issue')
  const [tab, setTab] = useState(0)
  const [page, setPage] = useState(1)
  const [typeFilter, setTypeFilter] = useState('')
  const [operation, setOperation] = useState<Operation>('receive')
  const [selectedBalance, setSelectedBalance] = useState<InventoryBalance | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [formError, setFormError] = useState('')
  const [thresholdDialogOpen, setThresholdDialogOpen] = useState(false)
  const [thresholdProductId, setThresholdProductId] = useState('')
  const [thresholdMinQty, setThresholdMinQty] = useState('')
  const [thresholdError, setThresholdError] = useState('')

  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => apiRequest<Product[]>('/api/products'),
    retry: false,
  })
  const balancesQuery = useQuery({
    queryKey: ['inventory-balances', selectedFarmId],
    queryFn: () =>
      apiRequest<InventoryBalance[]>(`/api/farms/${selectedFarmId}/inventory/balances`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const thresholdsQuery = useQuery({
    queryKey: ['inventory-thresholds', selectedFarmId],
    queryFn: () =>
      apiRequest<InventoryThreshold[]>(`/api/farms/${selectedFarmId}/inventory/thresholds`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const transactionParams = new URLSearchParams({
    page: page.toString(),
    pageSize: '20',
  })
  if (typeFilter) {
    transactionParams.set(
      'transactionType',
      transactionTypeValues[typeFilter as InventoryTransactionType].toString(),
    )
  }
  const transactionsQuery = useQuery({
    queryKey: ['inventory-transactions', selectedFarmId, page, typeFilter],
    queryFn: () =>
      apiRequest<PagedResponse<InventoryTransaction>>(
        `/api/farms/${selectedFarmId}/inventory/transactions?${transactionParams}`,
      ),
    enabled: Boolean(selectedFarmId && tab === 1),
    retry: false,
  })

  const transactionMutation = useMutation({
    mutationFn: (payload: InventoryPayload) => {
      const endpoint = operation === 'adjustment' ? 'adjustments' : operation
      return apiRequest(`/api/farms/${selectedFarmId}/inventory/${endpoint}`, {
        method: 'POST',
        body: JSON.stringify(payload),
      })
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory-balances', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['inventory-transactions', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['inventory-thresholds', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['notifications-summary', selectedFarmId] }),
      ])
      setFormOpen(false)
      setSelectedBalance(null)
    },
    onError: (caught) =>
      setFormError(
        caught instanceof ApiError ? caught.message : 'Could not record the inventory transaction.',
      ),
  })

  const saveThresholdMutation = useMutation({
    mutationFn: () =>
      apiRequest<InventoryThreshold>(
        `/api/farms/${selectedFarmId}/inventory/thresholds/${thresholdProductId}`,
        {
          method: 'PUT',
          body: JSON.stringify({ minimumQuantity: Number(thresholdMinQty) }),
        },
      ),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory-thresholds', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['notifications-summary', selectedFarmId] }),
      ])
      setThresholdDialogOpen(false)
    },
    onError: (caught) =>
      setThresholdError(
        caught instanceof ApiError ? caught.message : 'Could not save low-stock threshold.',
      ),
  })

  const deleteThresholdMutation = useMutation({
    mutationFn: (productId: string) =>
      apiRequest(`/api/farms/${selectedFarmId}/inventory/thresholds/${productId}`, {
        method: 'DELETE',
      }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory-thresholds', selectedFarmId] }),
        queryClient.invalidateQueries({ queryKey: ['notifications-summary', selectedFarmId] }),
      ])
    },
  })

  const openForm = (nextOperation: Operation, balance: InventoryBalance | null = null) => {
    setOperation(nextOperation)
    setSelectedBalance(balance)
    setFormError('')
    setFormOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view inventory.</Alert>
  }

  const pageError =
    productsQuery.error ?? balancesQuery.error ?? transactionsQuery.error

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Inventory
          </Typography>
          <Typography color="text.secondary">
            Track pesticide stock for {selectedFarm?.name}.
          </Typography>
        </Box>
        {canManage && (
          <Stack direction="row" spacing={1}>
            <Button
              variant="contained"
              startIcon={<AddRoundedIcon />}
              onClick={() => openForm('receive')}
            >
              Receive stock
            </Button>
            <Button
              variant="outlined"
              startIcon={<TuneRoundedIcon />}
              onClick={() => openForm('adjustment')}
            >
              Adjust
            </Button>
          </Stack>
        )}
      </Stack>

      {pageError && (
        <Alert severity="error">
          {pageError instanceof ApiError ? pageError.message : 'Could not load inventory.'}
        </Alert>
      )}

      {(thresholdsQuery.data ?? []).some((t) => t.isLowStock) && (
        <Alert severity="warning">
          {(thresholdsQuery.data ?? []).filter((t) => t.isLowStock).length} product(s) are currently below their configured minimum stock threshold.
        </Alert>
      )}

      <Card variant="outlined">
        <Tabs
          value={tab}
          onChange={(_, value: number) => setTab(value)}
          sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Balances" />
          <Tab label="Transactions" />
          <Tab label="Low-stock thresholds" />
        </Tabs>

        {tab === 0 &&
          (balancesQuery.isLoading ? (
            <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
              <CircularProgress />
            </Box>
          ) : !balancesQuery.data?.length ? (
            <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
              <Inventory2RoundedIcon color="primary" sx={{ fontSize: 48 }} />
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                No stock on hand
              </Typography>
              <Typography color="text.secondary">
                Receive a product batch to start tracking inventory.
              </Typography>
            </Stack>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell>Batch</TableCell>
                    <TableCell>Expiry</TableCell>
                    <TableCell align="right">On hand</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {balancesQuery.data.map((balance) => (
                    <TableRow key={`${balance.productId}-${balance.batchNumber}`} hover>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {balance.productName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip label={balance.batchNumber} size="small" variant="outlined" />
                      </TableCell>
                      <TableCell>{balance.expiryDate ?? '—'}</TableCell>
                      <TableCell align="right">
                        <Typography sx={{ fontWeight: 800 }}>{balance.quantityOnHand}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                          {canManage && (
                            <Button
                              size="small"
                              startIcon={<TuneRoundedIcon />}
                              onClick={() => openForm('adjustment', balance)}
                            >
                              Adjust
                            </Button>
                          )}
                          {canIssue && (
                            <Button
                              size="small"
                              color="warning"
                              startIcon={<RemoveRoundedIcon />}
                              onClick={() => openForm('issue', balance)}
                            >
                              Issue
                            </Button>
                          )}
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          ))}

        {tab === 1 && (
          <Box>
            <Stack direction="row" sx={{ p: 2.5, justifyContent: 'flex-end' }}>
              <FormControl size="small" sx={{ minWidth: 190 }}>
                <InputLabel id="transaction-filter-label">Transaction type</InputLabel>
                <Select
                  labelId="transaction-filter-label"
                  label="Transaction type"
                  value={typeFilter}
                  onChange={(event) => {
                    setTypeFilter(event.target.value)
                    setPage(1)
                  }}
                >
                  <MenuItem value="">All types</MenuItem>
                  {Object.keys(transactionTypeValues).map((type) => (
                    <MenuItem key={type} value={type}>
                      {type.replace(/([A-Z])/g, ' $1').trim()}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
            {transactionsQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : !transactionsQuery.data?.items.length ? (
              <Alert severity="info" sx={{ m: 2 }}>
                No inventory transactions match this filter.
              </Alert>
            ) : (
              <>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Date</TableCell>
                        <TableCell>Product</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Batch</TableCell>
                        <TableCell align="right">Quantity</TableCell>
                        <TableCell>Reference / reason</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {transactionsQuery.data.items.map((transaction) => (
                        <TableRow key={transaction.id} hover>
                          <TableCell>
                            {new Date(transaction.createdAtUtc).toLocaleString()}
                          </TableCell>
                          <TableCell>{transaction.productName}</TableCell>
                          <TableCell>
                            <Chip
                              size="small"
                              label={transaction.transactionType
                                .replace(/([A-Z])/g, ' $1')
                                .trim()}
                              color={transactionColor(transaction.transactionType)}
                            />
                          </TableCell>
                          <TableCell>{transaction.batchNumber ?? '—'}</TableCell>
                          <TableCell align="right">{transaction.quantity}</TableCell>
                          <TableCell>
                            {transaction.reference ?? transaction.reason ?? '—'}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
                <Stack sx={{ p: 2.5, alignItems: 'center' }}>
                  <Pagination
                    count={transactionsQuery.data.totalPages}
                    page={page}
                    onChange={(_, value) => setPage(value)}
                    color="primary"
                  />
                </Stack>
              </>
            )}
          </Box>
        )}

        {tab === 2 && (
          <Box sx={{ p: 2.5 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 2, gap: 1.5 }}
            >
              <Typography variant="body2" color="text.secondary">
                Set per-product minimum stock levels to trigger automatic low-stock notifications.
              </Typography>
              {canManage && (
                <Button
                  size="small"
                  variant="contained"
                  startIcon={<AddRoundedIcon />}
                  onClick={() => {
                    const activeProducts = (productsQuery.data ?? []).filter((p) => p.isActive)
                    setThresholdProductId(activeProducts[0]?.id ?? '')
                    setThresholdMinQty('')
                    setThresholdError('')
                    setThresholdDialogOpen(true)
                  }}
                >
                  Set threshold
                </Button>
              )}
            </Stack>
            {thresholdsQuery.isLoading ? (
              <Box sx={{ p: 5, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : !thresholdsQuery.data?.length ? (
              <Alert severity="info">
                No low-stock thresholds configured for this farm yet.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">On hand</TableCell>
                      <TableCell align="right">Minimum threshold</TableCell>
                      <TableCell>Status</TableCell>
                      {canManage && <TableCell align="right">Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {thresholdsQuery.data.map((threshold) => (
                      <TableRow key={threshold.id} hover>
                        <TableCell sx={{ fontWeight: 700 }}>{threshold.productName}</TableCell>
                        <TableCell align="right">{threshold.currentQuantityOnHand}</TableCell>
                        <TableCell align="right">{threshold.minimumQuantity}</TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={threshold.isLowStock ? 'Low stock' : 'OK'}
                            color={threshold.isLowStock ? 'warning' : 'success'}
                          />
                        </TableCell>
                        {canManage && (
                          <TableCell align="right">
                            <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                              <Button
                                size="small"
                                onClick={() => {
                                  setThresholdProductId(threshold.productId)
                                  setThresholdMinQty(String(threshold.minimumQuantity))
                                  setThresholdError('')
                                  setThresholdDialogOpen(true)
                                }}
                              >
                                Edit
                              </Button>
                              <Tooltip title="Remove threshold">
                                <IconButton
                                  size="small"
                                  aria-label={`Remove threshold for ${threshold.productName}`}
                                  onClick={() => deleteThresholdMutation.mutate(threshold.productId)}
                                  disabled={deleteThresholdMutation.isPending}
                                >
                                  <DeleteOutlineRoundedIcon fontSize="small" />
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
            )}
          </Box>
        )}
      </Card>

      <InventoryForm
        open={formOpen}
        operation={operation}
        balance={selectedBalance}
        products={(productsQuery.data ?? []).filter((product) => product.isActive)}
        saving={transactionMutation.isPending}
        error={formError}
        onClose={() => setFormOpen(false)}
        onSave={(payload) => transactionMutation.mutate(payload)}
      />

      <Dialog
        open={thresholdDialogOpen}
        onClose={saveThresholdMutation.isPending ? undefined : () => setThresholdDialogOpen(false)}
        fullWidth
        maxWidth="xs"
      >
        <Box
          component="form"
          onSubmit={(event) => {
            event.preventDefault()
            saveThresholdMutation.mutate()
          }}
        >
          <DialogTitle>Low-stock threshold</DialogTitle>
          <DialogContent>
            <Stack spacing={2.5} sx={{ pt: 1 }}>
              {thresholdError && <Alert severity="error">{thresholdError}</Alert>}
              <FormControl fullWidth required>
                <InputLabel id="threshold-product-label">Product</InputLabel>
                <Select
                  labelId="threshold-product-label"
                  label="Product"
                  value={thresholdProductId}
                  onChange={(event) => setThresholdProductId(event.target.value)}
                >
                  {(productsQuery.data ?? [])
                    .filter((p) => p.isActive)
                    .map((product) => (
                      <MenuItem key={product.id} value={product.id}>
                        {product.name}
                      </MenuItem>
                    ))}
                </Select>
              </FormControl>
              <TextField
                label="Minimum quantity"
                type="number"
                value={thresholdMinQty}
                onChange={(event) => setThresholdMinQty(event.target.value)}
                slotProps={{ htmlInput: { min: 0.001, step: 'any' } }}
                required
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 3, pt: 1 }}>
            <Button onClick={() => setThresholdDialogOpen(false)} disabled={saveThresholdMutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" disabled={saveThresholdMutation.isPending}>
              {saveThresholdMutation.isPending ? 'Saving…' : 'Save threshold'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Stack>
  )
}
