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
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
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
import AdminPanelSettingsRoundedIcon from '@mui/icons-material/AdminPanelSettingsRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import GroupRoundedIcon from '@mui/icons-material/GroupRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type { FarmRole, FarmUser, ManagedUser, PagedResponse } from '../types/api'

/* oxlint-disable react/set-state-in-effect -- dialog forms reset from selected records */

interface PermissionOption {
  code: string
  label: string
  description: string
}

const permissionGroups: { name: string; permissions: PermissionOption[] }[] = [
  {
    name: 'Farm and fields',
    permissions: [
      { code: 'farms.view', label: 'View farms', description: 'View farm information.' },
      { code: 'farms.manage', label: 'Manage farms', description: 'Update farm information.' },
      { code: 'fields.view', label: 'View fields', description: 'View field information.' },
      { code: 'fields.manage', label: 'Manage fields', description: 'Create and update fields.' },
      { code: 'crops.view', label: 'View crops', description: 'View crop information.' },
      { code: 'crops.manage', label: 'Manage crops', description: 'Manage crop assignments.' },
    ],
  },
  {
    name: 'Operations',
    permissions: [
      { code: 'workers.view', label: 'View workers', description: 'View farm workers.' },
      { code: 'workers.manage', label: 'Manage workers', description: 'Create and update workers.' },
      { code: 'weather.view', label: 'View weather', description: 'View weather records.' },
      { code: 'weather.manage', label: 'Manage weather', description: 'Create and update weather.' },
      {
        code: 'applications.view',
        label: 'View applications',
        description: 'View pesticide applications.',
      },
      {
        code: 'applications.create',
        label: 'Create applications',
        description: 'Create and edit application drafts.',
      },
      {
        code: 'applications.approve',
        label: 'Approve applications',
        description: 'Complete pesticide applications.',
      },
    ],
  },
  {
    name: 'Products and inventory',
    permissions: [
      { code: 'products.view', label: 'View products', description: 'View pesticide products.' },
      {
        code: 'products.manage',
        label: 'Manage products',
        description: 'Manage pesticide products.',
      },
      { code: 'inventory.view', label: 'View inventory', description: 'View stock and history.' },
      {
        code: 'inventory.manage',
        label: 'Manage inventory',
        description: 'Receive and adjust inventory.',
      },
      {
        code: 'inventory.issue',
        label: 'Issue inventory',
        description: 'Issue stock and complete applications.',
      },
    ],
  },
  {
    name: 'Administration and reporting',
    permissions: [
      {
        code: 'farm-users.view',
        label: 'View farm users',
        description: 'View farm assignments.',
      },
      {
        code: 'farm-users.manage',
        label: 'Manage farm users',
        description: 'Manage assignments and roles.',
      },
      { code: 'reports.view', label: 'View reports', description: 'View farm reports.' },
      { code: 'reports.export', label: 'Export reports', description: 'Export reports to CSV, Excel, and PDF.' },
      {
        code: 'compliance.view',
        label: 'View safety & compliance',
        description: 'View certification monitoring, safety intervals, and compliance records.',
      },
      {
        code: 'compliance.manage',
        label: 'Manage safety & compliance',
        description: 'Create and update compliance records and corrective actions.',
      },
      { code: 'audit.view', label: 'View audit trail', description: 'View farm audit records.' },
    ],
  },
]

interface RoleFormProps {
  open: boolean
  role: FarmRole | null
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: { name: string; description: string | null; permissionCodes: string[] }) => void
}

function RoleForm({ open, role, saving, error, onClose, onSave }: RoleFormProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [permissions, setPermissions] = useState<string[]>([])

  useEffect(() => {
    if (!open) return
    setName(role?.name ?? '')
    setDescription(role?.description ?? '')
    setPermissions(role?.permissionCodes ?? [])
  }, [open, role])

  const togglePermission = (code: string) => {
    setPermissions((current) =>
      current.includes(code) ? current.filter((item) => item !== code) : [...current, code],
    )
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      name: name.trim(),
      description: description.trim() || null,
      permissionCodes: permissions,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="md">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{role ? 'Edit farm role' : 'Create farm role'}</DialogTitle>
        <DialogContent>
          <Stack spacing={3} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Role name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              slotProps={{ htmlInput: { minLength: 2, maxLength: 100 } }}
              required
              autoFocus
            />
            <TextField
              label="Description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 300 } }}
              multiline
              minRows={2}
            />
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 750, mb: 1 }}>
                Permissions
              </Typography>
              <Stack spacing={2}>
                {permissionGroups.map((group) => (
                  <Card key={group.name} variant="outlined" sx={{ p: 2 }}>
                    <Typography variant="subtitle2" sx={{ mb: 1 }}>
                      {group.name}
                    </Typography>
                    <Box
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                        gap: 0.5,
                      }}
                    >
                      {group.permissions.map((permission) => (
                        <FormControlLabel
                          key={permission.code}
                          control={
                            <Checkbox
                              checked={permissions.includes(permission.code)}
                              onChange={() => togglePermission(permission.code)}
                            />
                          }
                          label={
                            <Box>
                              <Typography variant="body2" sx={{ fontWeight: 650 }}>
                                {permission.label}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {permission.description}
                              </Typography>
                            </Box>
                          }
                        />
                      ))}
                    </Box>
                  </Card>
                ))}
              </Stack>
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving || !permissions.length}>
            {saving ? 'Saving…' : role ? 'Save role' : 'Create role'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

interface UserFormProps {
  open: boolean
  assignment: FarmUser | null
  roles: FarmRole[]
  accounts: ManagedUser[]
  saving: boolean
  error: string
  onClose: () => void
  onSave: (payload: { userId?: string; farmRoleIds: string[] }) => void
}

function UserForm({
  open,
  assignment,
  roles,
  accounts,
  saving,
  error,
  onClose,
  onSave,
}: UserFormProps) {
  const [userId, setUserId] = useState('')
  const [roleIds, setRoleIds] = useState<string[]>([])
  const assignableRoles = roles.filter((role) => !role.isSystemRole)

  useEffect(() => {
    if (!open) return
    setUserId(assignment?.userId ?? '')
    setRoleIds(assignment?.roles.map((role) => role.id) ?? [])
  }, [open, assignment])

  const toggleRole = (roleId: string) => {
    setRoleIds((current) =>
      current.includes(roleId)
        ? current.filter((item) => item !== roleId)
        : [...current, roleId],
    )
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSave({
      ...(assignment ? {} : { userId: userId.trim() }),
      farmRoleIds: roleIds,
    })
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{assignment ? 'Update farm roles' : 'Assign user to farm'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            {assignment ? (
              <Alert severity="info">
                Updating roles for {assignment.displayName} ({assignment.email ?? assignment.userId})
              </Alert>
            ) : (
              <FormControl required>
                <InputLabel id="farm-user-account-label">Account</InputLabel>
                <Select
                  labelId="farm-user-account-label"
                  label="Account"
                  value={userId}
                  onChange={(event) => setUserId(event.target.value)}
                  autoFocus
                >
                  {accounts.map((account) => (
                    <MenuItem key={account.id} value={account.id}>
                      {account.displayName} · {account.email}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
            <Box>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                Farm roles
              </Typography>
              {!assignableRoles.length ? (
                <Alert severity="warning">Create a custom farm role before assigning users.</Alert>
              ) : (
                <Stack>
                  {assignableRoles.map((role) => (
                    <FormControlLabel
                      key={role.id}
                      control={
                        <Checkbox
                          checked={roleIds.includes(role.id)}
                          onChange={() => toggleRole(role.id)}
                        />
                      }
                      label={
                        <Box>
                          <Typography variant="body2" sx={{ fontWeight: 650 }}>
                            {role.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {role.description ?? `${role.permissionCodes.length} permissions`}
                          </Typography>
                        </Box>
                      }
                    />
                  ))}
                </Stack>
              )}
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={saving || !roleIds.length}>
            {saving ? 'Saving…' : assignment ? 'Update roles' : 'Assign user'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

export function AdministrationPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm } = useFarm()
  const [tab, setTab] = useState(0)
  const [roleOpen, setRoleOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<FarmRole | null>(null)
  const [roleError, setRoleError] = useState('')
  const [userOpen, setUserOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<FarmUser | null>(null)
  const [userError, setUserError] = useState('')

  const rolesKey = ['farm-roles', selectedFarmId]
  const usersKey = ['farm-users', selectedFarmId]
  const rolesQuery = useQuery({
    queryKey: rolesKey,
    queryFn: () => apiRequest<FarmRole[]>(`/api/farms/${selectedFarmId}/roles`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const usersQuery = useQuery({
    queryKey: usersKey,
    queryFn: () => apiRequest<FarmUser[]>(`/api/farms/${selectedFarmId}/users`),
    enabled: Boolean(selectedFarmId),
    retry: false,
  })
  const accountsQuery = useQuery({
    queryKey: ['managed-users', 'farm-assignment-options'],
    queryFn: () =>
      apiRequest<PagedResponse<ManagedUser>>(
        '/api/admin/users?isActive=true&pageSize=100',
      ),
    retry: false,
  })

  const roleMutation = useMutation({
    mutationFn: (payload: {
      name: string
      description: string | null
      permissionCodes: string[]
    }) =>
      apiRequest<FarmRole>(
        editingRole
          ? `/api/farms/${selectedFarmId}/roles/${editingRole.id}`
          : `/api/farms/${selectedFarmId}/roles`,
        {
          method: editingRole ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: rolesKey })
      setRoleOpen(false)
      setEditingRole(null)
    },
    onError: (caught) =>
      setRoleError(caught instanceof ApiError ? caught.message : 'Could not save the farm role.'),
  })

  const userMutation = useMutation({
    mutationFn: (payload: { userId?: string; farmRoleIds: string[] }) =>
      apiRequest<FarmUser>(
        editingUser
          ? `/api/farms/${selectedFarmId}/users/${editingUser.id}/roles`
          : `/api/farms/${selectedFarmId}/users`,
        {
          method: editingUser ? 'PUT' : 'POST',
          body: JSON.stringify(payload),
        },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: usersKey })
      setUserOpen(false)
      setEditingUser(null)
    },
    onError: (caught) =>
      setUserError(
        caught instanceof ApiError ? caught.message : 'Could not save the user assignment.',
      ),
  })

  const statusMutation = useMutation({
    mutationFn: (user: FarmUser) =>
      apiRequest(`/api/farms/${selectedFarmId}/users/${user.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !user.isActive }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  })

  const openRole = (role: FarmRole | null) => {
    setEditingRole(role)
    setRoleError('')
    setRoleOpen(true)
  }

  const openUser = (user: FarmUser | null) => {
    setEditingUser(user)
    setUserError('')
    setUserOpen(true)
  }

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to manage roles and user assignments.</Alert>
  }

  const pageError =
    rolesQuery.error ??
    usersQuery.error ??
    accountsQuery.error ??
    statusMutation.error

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
          Farm administration
        </Typography>
        <Typography color="text.secondary">
          Manage access to {selectedFarm?.name}.
        </Typography>
      </Box>

      {pageError && (
        <Alert severity="error">
          {pageError instanceof ApiError ? pageError.message : 'Could not load farm access data.'}
        </Alert>
      )}

      <Card variant="outlined">
        <Tabs
          value={tab}
          onChange={(_, value: number) => setTab(value)}
          sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<AdminPanelSettingsRoundedIcon />} iconPosition="start" label="Roles" />
          <Tab icon={<GroupRoundedIcon />} iconPosition="start" label="Users" />
        </Tabs>

        {tab === 0 && (
          <Box>
            <Stack direction="row" sx={{ p: 2.5, justifyContent: 'flex-end' }}>
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                onClick={() => openRole(null)}
              >
                Create role
              </Button>
            </Stack>
            {rolesQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Role</TableCell>
                      <TableCell>Permissions</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell align="right">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {(rolesQuery.data ?? []).map((role) => (
                      <TableRow key={role.id} hover>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {role.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {role.description}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                            {role.permissionCodes.map((code) => (
                              <Chip key={code} label={code} size="small" variant="outlined" />
                            ))}
                          </Stack>
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={role.isSystemRole ? 'System' : 'Custom'}
                            size="small"
                            color={role.isSystemRole ? 'primary' : 'default'}
                          />
                        </TableCell>
                        <TableCell align="right">
                          {!role.isSystemRole && (
                            <Tooltip title="Edit role">
                              <IconButton
                                onClick={() => openRole(role)}
                                aria-label={`Edit ${role.name}`}
                              >
                                <EditRoundedIcon />
                              </IconButton>
                            </Tooltip>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        )}

        {tab === 1 && (
          <Box>
            <Stack direction="row" sx={{ p: 2.5, justifyContent: 'flex-end' }}>
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                onClick={() => openUser(null)}
              >
                Assign user
              </Button>
            </Stack>
            {usersQuery.isLoading ? (
              <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
                <CircularProgress />
              </Box>
            ) : !(usersQuery.data ?? []).length ? (
              <Alert severity="info" sx={{ m: 2 }}>
                No users are assigned to this farm.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>User</TableCell>
                      <TableCell>Roles</TableCell>
                      <TableCell>Assigned</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell align="right">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {usersQuery.data?.map((user) => (
                      <TableRow key={user.id} hover>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {user.displayName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {user.email ?? 'No email'}
                          </Typography>
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{ display: 'block', fontFamily: 'monospace' }}
                          >
                            {user.userId}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                            {user.roles.map((role) => (
                              <Chip key={role.id} label={role.name} size="small" />
                            ))}
                          </Stack>
                        </TableCell>
                        <TableCell>{new Date(user.assignedAtUtc).toLocaleDateString()}</TableCell>
                        <TableCell>
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                            <Switch
                              size="small"
                              checked={user.isActive}
                              onChange={() => statusMutation.mutate(user)}
                              disabled={statusMutation.isPending}
                              slotProps={{
                                input: {
                                  'aria-label': `Toggle ${user.displayName} assignment`,
                                },
                              }}
                            />
                            <Typography variant="body2">
                              {user.isActive ? 'Active' : 'Inactive'}
                            </Typography>
                          </Stack>
                        </TableCell>
                        <TableCell align="right">
                          <Tooltip title="Update roles">
                            <IconButton
                              onClick={() => openUser(user)}
                              aria-label={`Update ${user.displayName} roles`}
                            >
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
          </Box>
        )}
      </Card>

      <RoleForm
        open={roleOpen}
        role={editingRole}
        saving={roleMutation.isPending}
        error={roleError}
        onClose={() => setRoleOpen(false)}
        onSave={(payload) => roleMutation.mutate(payload)}
      />
      <UserForm
        open={userOpen}
        assignment={editingUser}
        roles={rolesQuery.data ?? []}
        accounts={accountsQuery.data?.items ?? []}
        saving={userMutation.isPending}
        error={userError}
        onClose={() => setUserOpen(false)}
        onSave={(payload) => userMutation.mutate(payload)}
      />
    </Stack>
  )
}
