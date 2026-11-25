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
  MenuItem,
  Pagination,
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
import LockResetRoundedIcon from '@mui/icons-material/LockResetRounded'
import LockOpenRoundedIcon from '@mui/icons-material/LockOpenRounded'
import { ApiError, apiRequest } from '../lib/api'
import type { ManagedUser, PagedResponse } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog form resets from the selected account */

const systemRoles = ['SystemAdministrator', 'SystemAuditor']

interface UserDialogProps {
  open: boolean
  user: ManagedUser | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: {
    displayName: string
    email: string
    password?: string
    systemRoles: string[]
  }) => void
}

function UserDialog({ open, user, saving, error, onClose, onSave }: UserDialogProps) {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [roles, setRoles] = useState<string[]>([])

  useEffect(() => {
    if (!open) return
    setDisplayName(user?.displayName ?? '')
    setEmail(user?.email ?? '')
    setPassword('')
    setRoles(user?.systemRoles ?? [])
  }, [open, user])

  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      displayName: displayName.trim(),
      email: email.trim(),
      ...(user ? {} : { password }),
      systemRoles: roles,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={submit}>
        <DialogTitle>{user ? 'Edit account' : 'Create account'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Display name" value={displayName} onChange={(event) => setDisplayName(event.target.value)} required />
            <TextField label="Email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
            {!user && (
              <TextField
                label="Temporary password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                slotProps={{ htmlInput: { minLength: 8 } }}
                required
              />
            )}
            <Box>
              <Typography variant="subtitle2">System roles</Typography>
              {systemRoles.map((role) => (
                <FormControlLabel
                  key={role}
                  control={
                    <Checkbox
                      checked={roles.includes(role)}
                      onChange={() =>
                        setRoles((current) =>
                          current.includes(role)
                            ? current.filter((value) => value !== role)
                            : [...current, role],
                        )
                      }
                    />
                  }
                  label={role === 'SystemAdministrator' ? 'System administrator' : 'System auditor'}
                />
              ))}
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Saving…' : user ? 'Save account' : 'Create account'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function UserAdministrationPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [systemRole, setSystemRole] = useState('')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<ManagedUser | null>(null)
  const [dialogError, setDialogError] = useState('')
  const [resetUser, setResetUser] = useState<ManagedUser | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [actionError, setActionError] = useState('')
  const params = new URLSearchParams({ page: String(page), pageSize: '20' })
  if (search.trim()) params.set('search', search.trim())
  if (status) params.set('isActive', status)
  if (systemRole) params.set('systemRole', systemRole)
  const usersKey = ['managed-users', page, search, status, systemRole]

  const usersQuery = useQuery({
    queryKey: usersKey,
    queryFn: () => apiRequest<PagedResponse<ManagedUser>>(`/api/admin/users?${params}`),
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: async (payload: {
      displayName: string
      email: string
      password?: string
      systemRoles: string[]
    }) => {
      if (!editing) {
        return apiRequest<ManagedUser>('/api/admin/users', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      await apiRequest<ManagedUser>(`/api/admin/users/${editing.id}`, {
        method: 'PUT',
        body: JSON.stringify({ displayName: payload.displayName, email: payload.email }),
      })
      return apiRequest<ManagedUser>(`/api/admin/users/${editing.id}/system-roles`, {
        method: 'PUT',
        body: JSON.stringify({ systemRoles: payload.systemRoles }),
      })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['managed-users'] })
      setDialogOpen(false)
      setEditing(null)
    },
    onError: (error) =>
      setDialogError(error instanceof ApiError ? error.message : 'Could not save this account.'),
  })

  const statusMutation = useMutation({
    mutationFn: (user: ManagedUser) =>
      apiRequest(`/api/admin/users/${user.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !user.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['managed-users'] }),
    onError: (error) =>
      setActionError(error instanceof ApiError ? error.message : 'Could not change account status.'),
  })

  const unlockMutation = useMutation({
    mutationFn: (user: ManagedUser) =>
      apiRequest(`/api/admin/users/${user.id}/unlock`, { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['managed-users'] }),
    onError: (error) =>
      setActionError(error instanceof ApiError ? error.message : 'Could not unlock this account.'),
  })

  const resetMutation = useMutation({
    mutationFn: () =>
      apiRequest(`/api/admin/users/${resetUser?.id}/reset-password`, {
        method: 'POST',
        body: JSON.stringify({ newPassword }),
      }),
    onSuccess: () => {
      setResetUser(null)
      setNewPassword('')
    },
    onError: (error) =>
      setActionError(error instanceof ApiError ? error.message : 'Could not reset the password.'),
  })

  const openUser = (user: ManagedUser | null) => {
    setEditing(user)
    setDialogError('')
    setDialogOpen(true)
  }

  return (
    <Stack spacing={3}>
      <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 2 }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>User accounts</Typography>
          <Typography color="text.secondary">Create accounts and manage access, roles, lockouts, and recovery.</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => openUser(null)}>Create account</Button>
      </Stack>

      <Card variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            label="Search users"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            sx={{ flex: 1 }}
          />
          <TextField
            select
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value)
              setPage(1)
            }}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All accounts</MenuItem>
            <MenuItem value="true">Active</MenuItem>
            <MenuItem value="false">Inactive</MenuItem>
          </TextField>
          <TextField
            select
            label="System role"
            value={systemRole}
            onChange={(event) => {
              setSystemRole(event.target.value)
              setPage(1)
            }}
            sx={{ minWidth: 200 }}
          >
            <MenuItem value="">All system roles</MenuItem>
            <MenuItem value="SystemAdministrator">System administrator</MenuItem>
            <MenuItem value="SystemAuditor">System auditor</MenuItem>
          </TextField>
        </Stack>
      </Card>

      {(usersQuery.error || actionError) && (
        <Alert severity="error">
          {actionError ||
            (usersQuery.error instanceof ApiError
              ? usersQuery.error.message
              : 'Could not load user accounts.')}
        </Alert>
      )}

      <Card variant="outlined">
        {usersQuery.isLoading ? (
          <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow><TableCell>User</TableCell><TableCell>System roles</TableCell><TableCell>Security</TableCell><TableCell>Status</TableCell><TableCell align="right">Actions</TableCell></TableRow>
                </TableHead>
                <TableBody>
                  {usersQuery.data?.items.map((user) => (
                    <TableRow key={user.id} hover>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>{user.displayName}</Typography>
                        <Typography variant="caption" color="text.secondary">{user.email}</Typography>
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" sx={{ gap: 0.5, flexWrap: 'wrap' }}>
                          {user.systemRoles.length
                            ? user.systemRoles.map((role) => <Chip key={role} size="small" label={role.replace('System', '')} />)
                            : <Typography variant="caption" color="text.secondary">Farm user</Typography>}
                        </Stack>
                      </TableCell>
                      <TableCell>
                        {user.isLockedOut ? <Chip size="small" color="error" label="Locked" /> : `${user.accessFailedCount} failed attempts`}
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <Switch checked={user.isActive} onChange={() => statusMutation.mutate(user)} disabled={statusMutation.isPending} />
                          <Typography variant="body2">{user.isActive ? 'Active' : 'Inactive'}</Typography>
                        </Stack>
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="Edit account"><IconButton onClick={() => openUser(user)}><EditRoundedIcon /></IconButton></Tooltip>
                        <Tooltip title="Reset password"><IconButton onClick={() => { setResetUser(user); setNewPassword(''); setActionError('') }}><LockResetRoundedIcon /></IconButton></Tooltip>
                        {user.isLockedOut && (
                          <Tooltip title="Unlock account"><IconButton onClick={() => unlockMutation.mutate(user)}><LockOpenRoundedIcon /></IconButton></Tooltip>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            {usersQuery.data && (
              <Stack direction="row" sx={{ p: 2, justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">{usersQuery.data.totalCount} accounts</Typography>
                <Pagination count={usersQuery.data.totalPages} page={page} onChange={(_, value) => setPage(value)} />
              </Stack>
            )}
          </>
        )}
      </Card>

      <UserDialog
        open={dialogOpen}
        user={editing}
        saving={saveMutation.isPending}
        error={dialogError}
        onClose={() => setDialogOpen(false)}
        onSave={(payload) => saveMutation.mutate(payload)}
      />

      <Dialog open={Boolean(resetUser)} onClose={() => setResetUser(null)} fullWidth maxWidth="xs">
        <DialogTitle>Reset password for {resetUser?.displayName}</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="New password"
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            slotProps={{ htmlInput: { minLength: 8 } }}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={() => setResetUser(null)}>Cancel</Button>
          <Button variant="contained" onClick={() => resetMutation.mutate()} disabled={newPassword.length < 8 || resetMutation.isPending}>
            Reset password
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}
