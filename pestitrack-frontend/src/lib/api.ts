const MUTATING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])
const AUTH_PROBE_PATHS = new Set(['/api/auth/me', '/api/auth/login', '/api/auth/logout'])

let unauthorizedHandler: (() => void) | null = null

export function registerUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

function handleUnauthorized(path: string, status: number): void {
  if (status === 401 && unauthorizedHandler && !AUTH_PROBE_PATHS.has(path.split('?')[0])) {
    unauthorizedHandler()
  }
}

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
    const candidate = body as {
      message?: unknown
      title?: unknown
      errors?: unknown
    }
    if (Array.isArray(candidate.errors)) {
      const descriptions = candidate.errors
        .map((error) =>
          error && typeof error === 'object' && 'description' in error
            ? String(error.description)
            : '',
        )
        .filter(Boolean)
      if (descriptions.length) return descriptions.join(' ')
    }
    if (candidate.errors && typeof candidate.errors === 'object') {
      const validationMessages = Object.values(candidate.errors)
        .flatMap((value) => (Array.isArray(value) ? value : []))
        .filter((value): value is string => typeof value === 'string')
      if (validationMessages.length) return validationMessages.join(' ')
    }
    if (typeof candidate.message === 'string') return candidate.message
    if (typeof candidate.title === 'string') return candidate.title
  }

  if (status === 429) {
    return 'Too many requests. Please wait a moment and try again.'
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
    handleUnauthorized(path, response.status)
    throw new ApiError(errorMessage(body, response.status), response.status, body)
  }

  return body as T
}

export async function apiDownload(path: string, fallbackName: string): Promise<void> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { Accept: '*/*' },
  })

  if (!response.ok) {
    handleUnauthorized(path, response.status)
    const body = await readResponse(response)
    throw new ApiError(errorMessage(body, response.status), response.status, body)
  }

  const disposition = response.headers.get('content-disposition') ?? ''
  const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  const plainName = disposition.match(/filename="?([^";]+)"?/i)?.[1]
  const fileName = encodedName ? decodeURIComponent(encodedName) : plainName || fallbackName
  const url = URL.createObjectURL(await response.blob())
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}
