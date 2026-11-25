import { useEffect, useMemo, useState, type FormEvent } from 'react'
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
  Divider,
  FormControl,
  IconButton,
  InputAdornment,
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
import SearchRoundedIcon from '@mui/icons-material/SearchRounded'
import ScienceRoundedIcon from '@mui/icons-material/ScienceRounded'
import { useAuth } from '../auth/AuthContext'
import { ApiError, apiRequest } from '../lib/api'
import type { Product } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from the selected record */

type ProductPayload = Omit<Product, 'id' | 'isActive' | 'createdAtUtc'>

interface ProductFormProps {
  open: boolean
  product: Product | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: ProductPayload) => void
}

const emptyProduct: ProductPayload = {
  name: '',
  activeIngredient: null,
  manufacturer: null,
  registrationNumber: null,
  formulation: null,
  concentration: null,
  hazardClass: null,
  signalWord: null,
  registrationExpiryDate: null,
  preHarvestIntervalDays: null,
  reEntryIntervalHours: null,
  maxApplicationRate: null,
  maxApplicationRateUnit: null,
  maxWindSpeedMetersPerSecond: null,
  labelNotes: null,
  description: null,
}

function ProductForm({ open, product, saving, error, onClose, onSave }: ProductFormProps) {
  const [values, setValues] = useState<ProductPayload>(emptyProduct)

  useEffect(() => {
    if (!open) return
    setValues(
      product
        ? {
            name: product.name,
            activeIngredient: product.activeIngredient,
            manufacturer: product.manufacturer,
            registrationNumber: product.registrationNumber,
            formulation: product.formulation,
            concentration: product.concentration,
            hazardClass: product.hazardClass,
            signalWord: product.signalWord,
            registrationExpiryDate: product.registrationExpiryDate,
            preHarvestIntervalDays: product.preHarvestIntervalDays,
            reEntryIntervalHours: product.reEntryIntervalHours,
            maxApplicationRate: product.maxApplicationRate,
            maxApplicationRateUnit: product.maxApplicationRateUnit,
            maxWindSpeedMetersPerSecond: product.maxWindSpeedMetersPerSecond,
            labelNotes: product.labelNotes,
            description: product.description,
          }
        : emptyProduct,
    )
  }, [open, product])

  const setValue = (key: keyof ProductPayload, value: string) => {
    setValues((current) => ({ ...current, [key]: value || null }))
  }

  const setNumberValue = (
    key: 'preHarvestIntervalDays' | 'reEntryIntervalHours' | 'maxApplicationRate' | 'maxWindSpeedMetersPerSecond',
    value: string,
    integer = false,
  ) => {
    if (!value.trim()) {
      setValues((current) => ({ ...current, [key]: null }))
      return
    }
    const parsed = integer ? Number.parseInt(value, 10) : Number.parseFloat(value)
    setValues((current) => ({ ...current, [key]: Number.isNaN(parsed) ? null : parsed }))
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      ...values,
      name: values.name.trim(),
      activeIngredient: values.activeIngredient?.trim() || null,
      manufacturer: values.manufacturer?.trim() || null,
      registrationNumber: values.registrationNumber?.trim() || null,
      formulation: values.formulation?.trim() || null,
      concentration: values.concentration?.trim() || null,
      hazardClass: values.hazardClass?.trim() || null,
      signalWord: values.signalWord?.trim() || null,
      registrationExpiryDate: values.registrationExpiryDate || null,
      maxApplicationRateUnit: values.maxApplicationRateUnit?.trim() || null,
      labelNotes: values.labelNotes?.trim() || null,
      description: values.description?.trim() || null,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="md">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{product ? 'Edit product' : 'Add pesticide product'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Product name"
              value={values.name}
              onChange={(event) => setValue('name', event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 200 } }}
              required
              autoFocus
            />
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2.5,
              }}
            >
              <TextField
                label="Active ingredient"
                value={values.activeIngredient ?? ''}
                onChange={(event) => setValue('activeIngredient', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 300 } }}
              />
              <TextField
                label="Manufacturer"
                value={values.manufacturer ?? ''}
                onChange={(event) => setValue('manufacturer', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 200 } }}
              />
              <TextField
                label="Registration number"
                value={values.registrationNumber ?? ''}
                onChange={(event) => setValue('registrationNumber', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              <TextField
                label="Registration expiry date"
                type="date"
                value={values.registrationExpiryDate ?? ''}
                onChange={(event) => setValue('registrationExpiryDate', event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                label="Formulation"
                value={values.formulation ?? ''}
                onChange={(event) => setValue('formulation', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              <TextField
                label="Concentration"
                value={values.concentration ?? ''}
                onChange={(event) => setValue('concentration', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 150 } }}
              />
              <TextField
                label="Hazard class"
                value={values.hazardClass ?? ''}
                onChange={(event) => setValue('hazardClass', event.target.value)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              <FormControl fullWidth>
                <InputLabel id="signal-word-label">Signal word</InputLabel>
                <Select
                  labelId="signal-word-label"
                  label="Signal word"
                  value={values.signalWord ?? ''}
                  onChange={(event) => setValue('signalWord', event.target.value)}
                >
                  <MenuItem value="">None</MenuItem>
                  <MenuItem value="Caution">Caution</MenuItem>
                  <MenuItem value="Warning">Warning</MenuItem>
                  <MenuItem value="Danger">Danger</MenuItem>
                  <MenuItem value="Danger - Poison">Danger - Poison</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Divider />
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              Label safety restrictions
            </Typography>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                gap: 2.5,
              }}
            >
              <TextField
                label="Pre-harvest interval (days)"
                type="number"
                value={values.preHarvestIntervalDays ?? ''}
                onChange={(event) => setNumberValue('preHarvestIntervalDays', event.target.value, true)}
                slotProps={{ htmlInput: { min: 0, max: 3650, step: 1 } }}
                helperText="Minimum days between application and harvest"
              />
              <TextField
                label="Re-entry interval (hours)"
                type="number"
                value={values.reEntryIntervalHours ?? ''}
                onChange={(event) => setNumberValue('reEntryIntervalHours', event.target.value, true)}
                slotProps={{ htmlInput: { min: 0, max: 8760, step: 1 } }}
                helperText="Restricted entry period after application"
              />
              <TextField
                label="Max application rate"
                type="number"
                value={values.maxApplicationRate ?? ''}
                onChange={(event) => setNumberValue('maxApplicationRate', event.target.value)}
                slotProps={{ htmlInput: { min: 0.0001, step: 'any' } }}
              />
              <TextField
                label="Max rate unit"
                value={values.maxApplicationRateUnit ?? ''}
                onChange={(event) => setValue('maxApplicationRateUnit', event.target.value)}
                placeholder="e.g. L/ha or kg/ha"
                slotProps={{ htmlInput: { maxLength: 50 } }}
              />
              <TextField
                label="Max wind speed (m/s)"
                type="number"
                value={values.maxWindSpeedMetersPerSecond ?? ''}
                onChange={(event) => setNumberValue('maxWindSpeedMetersPerSecond', event.target.value)}
                slotProps={{ htmlInput: { min: 0, max: 100, step: '0.1' } }}
                helperText="Applications above this wind speed are blocked"
              />
            </Box>

            <TextField
              label="Label safety notes"
              value={values.labelNotes ?? ''}
              onChange={(event) => setValue('labelNotes', event.target.value)}
              slotProps={{ htmlInput: { maxLength: 2000 } }}
              multiline
              minRows={2}
            />
            <TextField
              label="Description"
              value={values.description ?? ''}
              onChange={(event) => setValue('description', event.target.value)}
              slotProps={{ htmlInput: { maxLength: 1000 } }}
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
            {saving ? 'Saving…' : product ? 'Save changes' : 'Add product'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function ProductsPage() {
  const queryClient = useQueryClient()
  const { isAdministrator } = useAuth()
  const [search, setSearch] = useState('')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)
  const [formError, setFormError] = useState('')
  const queryKey = ['products']

  const { data: products = [], isLoading, error } = useQuery({
    queryKey,
    queryFn: () => apiRequest<Product[]>('/api/products'),
    retry: false,
  })

  const visibleProducts = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return products
    return products.filter((product) =>
      [product.name, product.activeIngredient, product.manufacturer, product.registrationNumber]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(term)),
    )
  }, [products, search])

  const saveMutation = useMutation({
    mutationFn: (payload: ProductPayload) =>
      apiRequest<Product>(editingProduct ? `/api/products/${editingProduct.id}` : '/api/products', {
        method: editingProduct ? 'PUT' : 'POST',
        body: JSON.stringify(payload),
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey })
      setDialogOpen(false)
      setEditingProduct(null)
    },
    onError: (caught) =>
      setFormError(caught instanceof ApiError ? caught.message : 'Could not save the product.'),
  })

  const statusMutation = useMutation({
    mutationFn: (product: Product) =>
      apiRequest<Product>(`/api/products/${product.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !product.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })

  const openForm = (product: Product | null) => {
    setEditingProduct(product)
    setFormError('')
    setDialogOpen(true)
  }

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Products
          </Typography>
          <Typography color="text.secondary">
            Browse pesticide products and their safety information.
          </Typography>
        </Box>
        {isAdministrator && (
          <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => openForm(null)}>
            Add product
          </Button>
        )}
      </Stack>

      {(error || statusMutation.error) && (
        <Alert severity="error">
          {(error ?? statusMutation.error) instanceof ApiError
            ? (error ?? statusMutation.error as ApiError).message
            : 'Could not complete the product request.'}
        </Alert>
      )}

      <Card variant="outlined">
        <Box sx={{ p: 2.5 }}>
          <TextField
            placeholder="Search products"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            size="small"
            fullWidth
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchRoundedIcon />
                  </InputAdornment>
                ),
              },
            }}
          />
        </Box>
        {isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
            <CircularProgress />
          </Box>
        ) : !visibleProducts.length ? (
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <ScienceRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              No products found
            </Typography>
            <Typography color="text.secondary">
              {search ? 'Try a different search.' : 'The product catalog is empty.'}
            </Typography>
          </Stack>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Product</TableCell>
                  <TableCell>Active ingredient</TableCell>
                  <TableCell>Registration</TableCell>
                  <TableCell>Safety intervals & limits</TableCell>
                  <TableCell>Hazard</TableCell>
                  {isAdministrator && <TableCell align="right">Actions</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {visibleProducts.map((product) => (
                  <TableRow key={product.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {product.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {[product.manufacturer, product.formulation, product.concentration]
                          .filter(Boolean)
                          .join(' · ') || 'No formulation details'}
                      </Typography>
                    </TableCell>
                    <TableCell>{product.activeIngredient ?? '—'}</TableCell>
                    <TableCell>
                      <Typography variant="body2">{product.registrationNumber ?? '—'}</Typography>
                      {product.registrationExpiryDate && (
                        <Typography variant="caption" color="text.secondary">
                          Exp. {product.registrationExpiryDate}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                        {product.reEntryIntervalHours != null && (
                          <Chip size="small" variant="outlined" label={`REI ${product.reEntryIntervalHours}h`} />
                        )}
                        {product.preHarvestIntervalDays != null && (
                          <Chip size="small" variant="outlined" label={`PHI ${product.preHarvestIntervalDays}d`} />
                        )}
                        {product.maxApplicationRate != null && (
                          <Chip
                            size="small"
                            variant="outlined"
                            label={`Max ${product.maxApplicationRate}${product.maxApplicationRateUnit ? ` ${product.maxApplicationRateUnit}` : ''}`}
                          />
                        )}
                        {product.maxWindSpeedMetersPerSecond != null && (
                          <Chip size="small" variant="outlined" label={`Wind ≤ ${product.maxWindSpeedMetersPerSecond} m/s`} />
                        )}
                        {product.reEntryIntervalHours == null &&
                          product.preHarvestIntervalDays == null &&
                          product.maxApplicationRate == null &&
                          product.maxWindSpeedMetersPerSecond == null && '—'}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                        {product.signalWord && (
                          <Chip
                            label={product.signalWord}
                            size="small"
                            color={
                              product.signalWord.toLowerCase().includes('danger')
                                ? 'error'
                                : product.signalWord.toLowerCase().includes('warning')
                                  ? 'warning'
                                  : 'info'
                            }
                          />
                        )}
                        {product.hazardClass && (
                          <Chip label={product.hazardClass} size="small" variant="outlined" color="warning" />
                        )}
                        {!product.signalWord && !product.hazardClass && '—'}
                      </Stack>
                    </TableCell>
                    {isAdministrator && (
                      <TableCell align="right">
                        <Stack direction="row" sx={{ justifyContent: 'flex-end', alignItems: 'center' }}>
                          <Switch
                            size="small"
                            checked={product.isActive}
                            onChange={() => statusMutation.mutate(product)}
                            disabled={statusMutation.isPending}
                            slotProps={{
                              input: { 'aria-label': `Toggle ${product.name} status` },
                            }}
                          />
                          <Tooltip title="Edit product">
                            <IconButton
                              onClick={() => openForm(product)}
                              aria-label={`Edit ${product.name}`}
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
        )}
      </Card>

      <ProductForm
        open={dialogOpen}
        product={editingProduct}
        saving={saveMutation.isPending}
        error={formError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />
    </Stack>
  )
}
