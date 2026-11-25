import { useState, type FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useAuth } from '../auth/AuthContext'
import { ApiError, apiRequest } from '../lib/api'

export function AccountPage() {
  const { user } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [message, setMessage] = useState('')

  const mutation = useMutation({
    mutationFn: () =>
      apiRequest('/api/auth/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword }),
      }),
    onSuccess: () => {
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setMessage('Your password was changed successfully.')
    },
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setMessage('')
    mutation.mutate()
  }

  return (
    <Stack spacing={3} sx={{ maxWidth: 720 }}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>My account</Typography>
        <Typography color="text.secondary">Review your profile and update your password.</Typography>
      </Box>

      <Card variant="outlined">
        <CardContent>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Avatar sx={{ width: 56, height: 56, bgcolor: 'primary.main' }}>
              {user?.displayName.charAt(0).toUpperCase()}
            </Avatar>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 750 }}>{user?.displayName}</Typography>
              <Typography color="text.secondary">{user?.email}</Typography>
              <Typography variant="caption" color="text.secondary">
                {user?.systemRoles.length ? user.systemRoles.join(', ') : 'Farm user'}
              </Typography>
            </Box>
          </Stack>
        </CardContent>
      </Card>

      <Card variant="outlined">
        <CardContent>
          <Typography variant="h6" sx={{ fontWeight: 750, mb: 2 }}>Change password</Typography>
          <Box component="form" onSubmit={submit}>
            <Stack spacing={2}>
              {message && <Alert severity="success">{message}</Alert>}
              {mutation.error && (
                <Alert severity="error">
                  {mutation.error instanceof ApiError
                    ? mutation.error.message
                    : 'Could not change your password.'}
                </Alert>
              )}
              <TextField label="Current password" type="password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} required />
              <TextField label="New password" type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} slotProps={{ htmlInput: { minLength: 8 } }} helperText="At least 8 characters" required />
              <TextField
                label="Confirm new password"
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                error={Boolean(confirmPassword && confirmPassword !== newPassword)}
                helperText={confirmPassword && confirmPassword !== newPassword ? 'Passwords do not match.' : ' '}
                required
              />
              <Button type="submit" variant="contained" sx={{ alignSelf: 'flex-start' }} disabled={mutation.isPending || newPassword.length < 8 || newPassword !== confirmPassword}>
                {mutation.isPending ? 'Changing…' : 'Change password'}
              </Button>
            </Stack>
          </Box>
        </CardContent>
      </Card>
    </Stack>
  )
}
