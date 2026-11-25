import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Box, Button, Card, Stack, Typography } from '@mui/material'
import ErrorOutlineRoundedIcon from '@mui/icons-material/ErrorOutlineRounded'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error:', error, info)
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children
    }

    return (
      <Box
        sx={{
          minHeight: '100vh',
          display: 'grid',
          placeItems: 'center',
          p: 3,
          bgcolor: 'background.default',
        }}
      >
        <Card variant="outlined" sx={{ maxWidth: 480, p: 4, textAlign: 'center' }}>
          <Stack spacing={2} sx={{ alignItems: 'center' }}>
            <ErrorOutlineRoundedIcon color="error" sx={{ fontSize: 48 }} />
            <Typography variant="h5" sx={{ fontWeight: 800 }}>
              Something went wrong
            </Typography>
            <Typography variant="body2" color="text.secondary">
              An unexpected error interrupted this screen. Reloading usually resolves the issue.
            </Typography>
            <Stack direction="row" spacing={1.5} sx={{ pt: 1 }}>
              <Button variant="outlined" onClick={() => window.location.assign('/')}>
                Go to overview
              </Button>
              <Button variant="contained" onClick={() => window.location.reload()}>
                Reload page
              </Button>
            </Stack>
          </Stack>
        </Card>
      </Box>
    )
  }
}
