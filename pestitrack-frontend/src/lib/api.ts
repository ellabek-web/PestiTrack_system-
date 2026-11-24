const MUTATING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])

export class ApiError extends Error {
  readonly status: number
  readonly details?: unknown

  constructor(
    message: string,
    status: number,
    details?: unknown,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

async function readResponse(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined

  const contentType = response.headers.get('content-type')
  if (contentType?.includes('application/json')) return response.json()

  const text = await response.text()
  return text || undefined
}

function errorMessage(body: unknown, status: number): string {
  if (body && typeof body === 'object') {
    const candidate = body as { message?: unknown; title?: unknown }
    if (typeof candidate.message === 'string') return candidate.message
    if (typeof candidate.title === 'string') return candidate.title
  }

  return `Request failed (${status})`
}

async function csrfToken(): Promise<string> {
  const response = await fetch('/api/auth/csrf', {
    credentials: 'include',
    headers: { Accept: 'application/json' },
  })
  const body = (await readResponse(response)) as { requestToken?: string }

  if (!response.ok || !body?.requestToken) {
    throw new ApiError('Could not initialize a secure request.', response.status, body)
  }

  return body.requestToken
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')

  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  if (MUTATING_METHODS.has(method)) {
    headers.set('X-CSRF-TOKEN', await csrfToken())
  }

  const response = await fetch(path, {
    ...options,
    method,
    headers,
    credentials: 'include',
  })
  const body = await readResponse(response)

  if (!response.ok) {
    throw new ApiError(errorMessage(body, response.status), response.status, body)
  }

  return body as T
}
