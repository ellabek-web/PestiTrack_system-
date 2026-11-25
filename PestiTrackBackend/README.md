# PestiTrack backend

ASP.NET Core API for farm pesticide operations.

## Application history

The paginated history endpoint is:

`GET /api/farms/{farmId}/applications/history`

Supported query parameters:

- `fieldId`, `fieldCropId`, `workerId`, `productId`
- `status` (`Draft` or `Completed`)
- `fromUtc`, `toUtc`
- `search` (target pest, method, equipment, field, crop, worker, product, or batch)
- `page` (default `1`)
- `pageSize` (default `20`, maximum `100`)

The response includes pagination metadata, creator information, product counts, and
whether every product line has a linked inventory transaction.

`GET /api/farms/{farmId}/applications/{applicationId}` returns the detailed
application, including its creator, weather snapshot, and the inventory transaction
linked to each product line.

Both endpoints require the farm permission `applications.view`.

## Farm dashboard

`GET /api/farms/{farmId}/permissions` returns the current authenticated user's
effective permission codes for a farm (system administrators receive the full
permission catalog).

`GET /api/dashboard` provides system-wide totals for farms, fields, active crop
assignments, workers, stocked products, and applications. It is restricted to
the `SystemAdministrator` role.

`GET /api/farms/{farmId}/dashboard` returns farm identity information and
operational summaries for fields, crop assignments, workers, inventory, and
pesticide applications.

The endpoint requires `farms.view`. Each optional section is returned only when
the current farm user also has its corresponding view permission:

- `fields.view` — field totals and active area
- `crops.view` — crop assignment status counts
- `workers.view` — worker, applicator, and certification counts
- `inventory.view` — stock-line, product, batch, and expiry summaries plus recent transactions
- `applications.view` — application counts, recent treated area, and recent applications

This section-level authorization prevents dashboard aggregation from exposing
module data the current user cannot otherwise view.

## Reports and CSV exports

Farm reports require `reports.view`:

- `GET /api/farms/{farmId}/reports/applications`
- `GET /api/farms/{farmId}/reports/inventory`
- `GET /api/farms/{farmId}/reports/product-usage`
- `GET /api/farms/{farmId}/reports/worker-activity`

Application reports can be filtered by UTC date range, field, worker, product,
and application status. Inventory reports support UTC date range, product,
transaction type, and batch filters. Detailed reports are paginated with a
maximum page size of 200.

CSV exports require both `reports.view` and `reports.export`:

- `GET /api/farms/{farmId}/reports/applications/export/csv`
- `GET /api/farms/{farmId}/reports/inventory/export/csv`
- `GET /api/farms/{farmId}/reports/product-usage/export/csv`
- `GET /api/farms/{farmId}/reports/worker-activity/export/csv`

Exports are limited to 10,000 rows and neutralize spreadsheet formulas in text
fields.

Excel and PDF exports use the same filters and permissions. Replace the final
`csv` segment with `xlsx` or `pdf`, for example:

- `GET /api/farms/{farmId}/reports/applications/export/xlsx`
- `GET /api/farms/{farmId}/reports/applications/export/pdf`

The same format suffix is supported for inventory, product-usage, and
worker-activity reports. XLSX files include styled headers, frozen report
headings, filters, and calculated column widths. PDF files use a paginated
landscape layout. Both formats are generated with built-in .NET libraries and
do not require third-party runtime or licensing configuration.

## User account administration

System administrators can manage the complete internal account lifecycle under
`/api/admin/users`:

- `GET /api/admin/users` — paginated search and status/role filters
- `GET /api/admin/users/{userId}` — account details
- `POST /api/admin/users` — create an account and optionally assign system roles
- `PUT /api/admin/users/{userId}` — update display name and login email
- `PATCH /api/admin/users/{userId}/status` — activate or deactivate an account
- `PUT /api/admin/users/{userId}/system-roles` — replace system roles
- `POST /api/admin/users/{userId}/reset-password` — administrative password recovery
- `POST /api/admin/users/{userId}/unlock` — clear lockout and failed-login state

Authenticated users can change their own password with
`POST /api/auth/change-password`.

The API prevents self-deactivation, self-removal of the administrator role, and
deactivation or demotion of the last active system administrator. Password,
role, and account-status changes update the Identity security stamp. Active
cookies are revalidated within one minute, invalidating sessions whose security
stamp changed.

## Audit trail

All Entity Framework business and account changes create an audit record in the
same database transaction as the change. Records identify the actor, farm,
operation, entity and key, changed values, UTC time, request path, IP address,
and HTTP method. Identity secrets such as password hashes, security stamps, and
recovery material are never recorded.

Farm users with `audit.view` can access farm-scoped history:

- `GET /api/farms/{farmId}/audit-logs`
- `GET /api/farms/{farmId}/audit-logs/{auditLogId}`

System administrators and system auditors can access cross-farm and
administrative history:

- `GET /api/admin/audit-logs`
- `GET /api/admin/audit-logs/{auditLogId}`

List endpoints support `fromUtc`, `toUtc`, `actorUserId`, `action`,
`entityType`, `search`, `page`, and `pageSize`. The system endpoint additionally
supports `farmId`. Audit records are append-only through the API.

## Safety and compliance

Products support label safety restrictions (`signalWord`,
`registrationExpiryDate`, `preHarvestIntervalDays`, `reEntryIntervalHours`,
`maxApplicationRate`, `maxApplicationRateUnit`, `maxWindSpeedMetersPerSecond`,
and `labelNotes`).

Pesticide applications enforce product registration validity, label maximum
application rates, weather wind-speed limits, and batch expiry on completion,
and expose a live safety evaluation endpoint:

- `GET /api/farms/{farmId}/applications/{applicationId}/safety-check` (`applications.view`)

Farm-scoped compliance monitoring and inspection/incident tracking endpoints:

- `GET /api/farms/{farmId}/compliance/overview` (`compliance.view`) — applicator certification statuses, active REI holds, active PHI holds (including expected-harvest conflicts), expiring/expired product registrations, and compliance record counts
- `GET /api/farms/{farmId}/compliance/records` (`compliance.view`) — filtered, paginated compliance records
- `POST /api/farms/{farmId}/compliance/records` (`compliance.manage`) — create compliance record
- `PUT /api/farms/{farmId}/compliance/records/{recordId}` (`compliance.manage`) — update compliance record
- `PATCH /api/farms/{farmId}/compliance/records/{recordId}/status` (`compliance.manage`) — update status and corrective action

## Notifications, low-stock thresholds, and live weather sync

- **Low-stock thresholds** (`inventory.view` / `inventory.manage`):
  - `GET /api/farms/{farmId}/inventory/thresholds`
  - `PUT /api/farms/{farmId}/inventory/thresholds/{productId}`
  - `DELETE /api/farms/{farmId}/inventory/thresholds/{productId}`
- **Farm notifications** (filtered automatically by the signed-in user's farm permissions):
  - `GET /api/farms/{farmId}/notifications` — synchronizes and returns paginated alerts for low stock, expiring/expired batches, expiring/expired applicator certifications, active REI holds, PHI harvest conflicts, product registrations, and overdue/non-compliant compliance records
  - `GET /api/farms/{farmId}/notifications/unread-count`
  - `PATCH /api/farms/{farmId}/notifications/{notificationId}/read`
  - `POST /api/farms/{farmId}/notifications/mark-all-read`
- **External weather integration (Open-Meteo)**:
  - Farms support optional `latitude` and `longitude` coordinates.
  - `POST /api/farms/{farmId}/weather-records/sync` (`weather.manage`) — fetches real-time conditions from Open-Meteo using request or farm coordinates and stores a `WeatherRecord`.

## Security hardening and health checks

- **Global exception handling**: Unhandled exceptions are converted to RFC 7807 `ProblemDetails` JSON without leaking internal stack traces or database details in non-development environments.
- **Security headers**: Adds `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `X-Permitted-Cross-Domain-Policies`, `Cache-Control: no-store` on `/api/*`, and `HSTS` in non-development environments.
- **Rate limiting**: Per-IP global rate limiting (`RateLimiting:GlobalPermitLimitPerMinute`, default `200/min`) and stricter authentication rate limiting (`RateLimiting:AuthPermitLimitPerMinute`, default `10/min`) on `/api/auth/login` and `/api/auth/change-password`.
- **Health probes**:
  - `GET /health/live` — liveness probe
  - `GET /health/ready` — readiness probe verifying SQL Server connectivity
- **Reverse-proxy & CORS readiness**: `ForwardedHeaders` enabled (`X-Forwarded-For`, `X-Forwarded-Proto`) and optional credentialed `Cors:AllowedOrigins` configuration.

## Development commands

```powershell
dotnet ef database update --project PestiTrackBackend\PestiTrackBackend.csproj
dotnet test PestiTrackBackend.Tests\PestiTrackBackend.Tests.csproj
dotnet run --project PestiTrackBackend\PestiTrackBackend.csproj --launch-profile https
```
