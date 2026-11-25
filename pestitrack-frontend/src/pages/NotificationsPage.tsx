import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Pagination,
  Select,
  Stack,
  Switch,
  Typography,
} from '@mui/material'
import DoneAllRoundedIcon from '@mui/icons-material/DoneAllRounded'
import MarkEmailReadRoundedIcon from '@mui/icons-material/MarkEmailReadRounded'
import MarkEmailUnreadRoundedIcon from '@mui/icons-material/MarkEmailUnreadRounded'
import NotificationsNoneRoundedIcon from '@mui/icons-material/NotificationsNoneRounded'
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded'
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded'
import { useFarm } from '../farms/FarmContext'
import { ApiError, apiRequest } from '../lib/api'
import type {
  FarmNotification,
  NotificationCategory,
  NotificationListResponse,
  NotificationSeverity,
} from '../types/api'

const categories: NotificationCategory[] = [
  'LowStock',
  'ExpiringBatch',
  'ExpiredBatch',
  'ExpiringCertification',
  'ExpiredCertification',
  'ProductRegistration',
  'ReEntryHold',
  'PreHarvestConflict',
  'ComplianceAlert',
]

const severities: NotificationSeverity[] = ['Critical', 'Warning', 'Info']

function formatLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function severityColor(severity: string): 'error' | 'warning' | 'info' {
  if (severity === 'Critical') return 'error'
  if (severity === 'Warning') return 'warning'
  return 'info'
}

export function NotificationsPage() {
  const queryClient = useQueryClient()
  const { selectedFarmId, selectedFarm } = useFarm()
  const [category, setCategory] = useState('')
  const [severity, setSeverity] = useState('')
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [includeResolved, setIncludeResolved] = useState(false)
  const [page, setPage] = useState(1)

  const query = useQuery({
    queryKey: [
      'notifications',
      selectedFarmId,
      category,
      severity,
      unreadOnly,
      includeResolved,
      page,
    ],
    queryFn: () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: '20',
      })
      if (category) params.set('category', category)
      if (severity) params.set('severity', severity)
      if (unreadOnly) params.set('unreadOnly', 'true')
      if (includeResolved) params.set('includeResolved', 'true')
      return apiRequest<NotificationListResponse>(
        `/api/farms/${selectedFarmId}/notifications?${params.toString()}`,
      )
    },
    enabled: Boolean(selectedFarmId),
    retry: false,
  })

  const invalidateAll = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['notifications', selectedFarmId] }),
      queryClient.invalidateQueries({ queryKey: ['notifications-summary', selectedFarmId] }),
    ])
  }

  const markReadMutation = useMutation({
    mutationFn: ({ id, isRead }: { id: string; isRead: boolean }) =>
      apiRequest<FarmNotification>(
        `/api/farms/${selectedFarmId}/notifications/${id}/read`,
        {
          method: 'PATCH',
          body: JSON.stringify({ isRead }),
        },
      ),
    onSuccess: invalidateAll,
  })

  const markAllMutation = useMutation({
    mutationFn: () =>
      apiRequest(`/api/farms/${selectedFarmId}/notifications/mark-all-read`, {
        method: 'POST',
      }),
    onSuccess: invalidateAll,
  })

  if (!selectedFarmId) {
    return <Alert severity="info">Select a farm to view alerts and notifications.</Alert>
  }

  const data = query.data

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800 }} gutterBottom>
            Notifications
          </Typography>
          <Typography color="text.secondary">
            {selectedFarm
              ? `Low-stock, expiry, certification, and safety alerts for ${selectedFarm.name}.`
              : 'Live operational and compliance alerts.'}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<RefreshRoundedIcon />}
            onClick={() => void invalidateAll()}
            disabled={query.isFetching}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<DoneAllRoundedIcon />}
            onClick={() => markAllMutation.mutate()}
            disabled={markAllMutation.isPending || !data?.unreadCount}
          >
            Mark all read
          </Button>
        </Stack>
      </Stack>

      <Card variant="outlined" sx={{ p: 2.5 }}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          sx={{ alignItems: { md: 'center' }, flexWrap: 'wrap' }}
        >
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel id="notif-category-label">Category</InputLabel>
            <Select
              labelId="notif-category-label"
              label="Category"
              value={category}
              onChange={(event) => {
                setCategory(event.target.value)
                setPage(1)
              }}
            >
              <MenuItem value="">All categories</MenuItem>
              {categories.map((item) => (
                <MenuItem key={item} value={item}>
                  {formatLabel(item)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel id="notif-severity-label">Severity</InputLabel>
            <Select
              labelId="notif-severity-label"
              label="Severity"
              value={severity}
              onChange={(event) => {
                setSeverity(event.target.value)
                setPage(1)
              }}
            >
              <MenuItem value="">All severities</MenuItem>
              {severities.map((item) => (
                <MenuItem key={item} value={item}>
                  {item}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControlLabel
            control={
              <Switch
                checked={unreadOnly}
                onChange={(event) => {
                  setUnreadOnly(event.target.checked)
                  setPage(1)
                }}
              />
            }
            label="Unread only"
          />

          <FormControlLabel
            control={
              <Switch
                checked={includeResolved}
                onChange={(event) => {
                  setIncludeResolved(event.target.checked)
                  setPage(1)
                }}
              />
            }
            label="Show resolved"
          />
        </Stack>
      </Card>

      {query.isLoading ? (
        <Box sx={{ p: 6, display: 'grid', placeItems: 'center' }}>
          <CircularProgress />
        </Box>
      ) : query.error ? (
        <Alert severity="error">
          {query.error instanceof ApiError
            ? query.error.message
            : 'Could not load notifications.'}
        </Alert>
      ) : !data?.items.length ? (
        <Card variant="outlined">
          <Stack sx={{ p: 6, alignItems: 'center', textAlign: 'center' }} spacing={1.5}>
            <NotificationsNoneRoundedIcon color="primary" sx={{ fontSize: 48 }} />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              All clear
            </Typography>
            <Typography color="text.secondary">
              No notifications match your current filters.
            </Typography>
          </Stack>
        </Card>
      ) : (
        <Stack spacing={1.5}>
          {data.items.map((item) => (
            <Card
              key={item.id}
              variant="outlined"
              sx={{
                p: 2.5,
                opacity: item.isResolved ? 0.72 : 1,
                borderLeft: 4,
                borderLeftColor: item.isResolved
                  ? 'success.main'
                  : item.severity === 'Critical'
                    ? 'error.main'
                    : item.severity === 'Warning'
                      ? 'warning.main'
                      : 'info.main',
                bgcolor: item.isRead || item.isResolved ? 'background.paper' : 'action.hover',
              }}
            >
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' }, gap: 2 }}
              >
                <Box sx={{ flex: 1 }}>
                  <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 0.75 }}>
                    <Chip
                      size="small"
                      label={item.severity}
                      color={severityColor(item.severity)}
                    />
                    <Chip
                      size="small"
                      variant="outlined"
                      label={formatLabel(item.category)}
                    />
                    {item.isResolved && (
                      <Chip size="small" label="Resolved" color="success" />
                    )}
                    {!item.isRead && !item.isResolved && (
                      <Chip size="small" label="Unread" color="primary" variant="outlined" />
                    )}
                  </Stack>
                  <Typography variant="subtitle1" sx={{ fontWeight: 750 }}>
                    {item.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                    {item.message}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
                    Updated {new Date(item.lastEvaluatedAtUtc).toLocaleString()}
                  </Typography>
                </Box>

                <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
                  {item.actionPath && (
                    <Button
                      size="small"
                      variant="outlined"
                      component={RouterLink}
                      to={item.actionPath}
                      endIcon={<OpenInNewRoundedIcon fontSize="small" />}
                    >
                      Open
                    </Button>
                  )}
                  {!item.isResolved && (
                    <Button
                      size="small"
                      startIcon={
                        item.isRead ? (
                          <MarkEmailUnreadRoundedIcon fontSize="small" />
                        ) : (
                          <MarkEmailReadRoundedIcon fontSize="small" />
                        )
                      }
                      onClick={() =>
                        markReadMutation.mutate({ id: item.id, isRead: !item.isRead })
                      }
                      disabled={markReadMutation.isPending}
                    >
                      {item.isRead ? 'Mark unread' : 'Mark read'}
                    </Button>
                  )}
                </Stack>
              </Stack>
            </Card>
          ))}

          {data.totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', pt: 1 }}>
              <Pagination
                count={data.totalPages}
                page={page}
                onChange={(_, next) => setPage(next)}
                color="primary"
              />
            </Box>
          )}
        </Stack>
      )}
    </Stack>
  )
}
