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
  IconButton,
  InputAdornment,
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
            description: product.description,
          }
        : emptyProduct,
    )
  }, [open, product])

  const setValue = (key: keyof ProductPayload, value: string) => {
    setValues((current) => ({ ...current, [key]: value || null }))
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
            </Box>
            <TextField
              label="Description"
              value={values.description ?? ''}
              onChange={(event) => setValue('description', event.target.value)}
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
                  <TableCell>Manufacturer</TableCell>
                  <TableCell>Registration</TableCell>
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
                        {[product.formulation, product.concentration].filter(Boolean).join(' · ') ||
                          'No formulation details'}
                      </Typography>
                    </TableCell>
                    <TableCell>{product.activeIngredient ?? '—'}</TableCell>
                    <TableCell>{product.manufacturer ?? '—'}</TableCell>
                    <TableCell>{product.registrationNumber ?? '—'}</TableCell>
                    <TableCell>
                      {product.hazardClass ? (
                        <Chip label={product.hazardClass} size="small" color="warning" />
                      ) : (
                        '—'
                      )}
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
