# PestiTrack Deployment Guide

PestiTrack uses host-only, `Secure`, `HttpOnly` cookies (`__Host-PestiTrack.Auth` and `__Host-PestiTrack.Csrf`) with `SameSite=Lax`/`Strict`. Both deployment paths below preserve **same-origin HTTPS** so cookies and CSRF validation work securely without weakening browser protections.

## Choosing a database

|  | SQL Server (local) | PostgreSQL (cloud) |
|---|---|---|
| When | Local development, on-prem | **Free online deployment** |
| Provider | `SqlServer` | `PostgreSql` |
| Connection | `ConnectionStrings:DefaultConnection` — a SQL Server string | `DATABASE_URL` — a `postgres://` URL |
| Migrations | `Data/Migrations` (15 migrations, unchanged) | `Data/Migrations/Postgres` (`InitialPostgres`) |

The API picks the provider automatically: if `DATABASE_URL` is a `postgres://` URL it uses PostgreSQL; otherwise it uses `Database:Provider` (`DATABASE_PROVIDER` env var, `SqlServer` / `PostgreSql` / `Auto`), and with `Auto` it detects the provider from the connection string. The same code, the same controllers — only the connection string changes. See `PestiTrackBackend/Data/DatabaseProvider.cs`.

Login sessions survive restarts and redeploys: the ASP.NET Core Data Protection keys are stored in the `DataProtectionKeys` table (migration `AddDataProtectionKeys`, both providers) instead of the container filesystem, via a small `IXmlRepository` (`PestiTrackBackend/Security/DataProtection/DatabaseDataProtectionKeyRepository.cs`, wired in `Program.cs`). The one redeploy that ships this logs everyone out once — the old container keys are lost — after that, sessions survive restarts. The 30-minute sliding cookie expiry is unchanged.

> All timestamps in the app are UTC (`DateTime.UtcNow`, `DateTimeOffset` in DTOs). PostgreSQL stores them as `timestamp with time zone`, SQL Server as `datetime2` — the values are identical on both.

---

## Part 1: Run Full Stack Locally with SQL Server (Docker Compose)

### 1. Configure environment variables
Copy `.env.example` to `.env` and set a strong `SQL_SA_PASSWORD`:

```powershell
Copy-Item .env.example .env
```

### 2. Build and start containers
```powershell
docker compose up -d --build
```
The backend entrypoint applies EF Core migrations on start, so no separate migrate step is needed. (To skip that, set `RUN_MIGRATIONS_ON_STARTUP=false`.)

### 3. Provision the initial System Administrator
Set `PESTITRACK_BOOTSTRAP_ADMIN_EMAIL`, `PESTITRACK_BOOTSTRAP_ADMIN_PASSWORD`, and `PESTITRACK_BOOTSTRAP_ADMIN_DISPLAY_NAME` (or pass them on the command line) and run `--bootstrap-admin`:

```powershell
docker compose run --rm `
  -e PESTITRACK_BOOTSTRAP_ADMIN_EMAIL="admin@example.com" `
  -e PESTITRACK_BOOTSTRAP_ADMIN_PASSWORD="ChangeMe_Admin123!" `
  -e PESTITRACK_BOOTSTRAP_ADMIN_DISPLAY_NAME="System Administrator" `
  backend dotnet PestiTrackBackend.dll --bootstrap-admin
```

### 4. Verify
- Open `https://localhost:8443` (accept the auto-generated local self-signed certificate, or mount real `tls.crt` / `tls.key` into `/etc/nginx/certs`).
- Health checks: `https://localhost:8443/health/live` and `https://localhost:8443/health/ready`.

---

## Part 2: Run Full Stack Locally with PostgreSQL (Docker Compose)

Same stack, same image — PostgreSQL instead of SQL Server. This is the local equivalent of the free cloud setup in Part 3, and the way to test the Postgres path before deploying it.

```powershell
Copy-Item .env.example .env   # set PG_PASSWORD in .env
docker compose -f docker-compose.postgres.yml up -d --build
docker compose -f docker-compose.postgres.yml exec backend dotnet PestiTrackBackend.dll --bootstrap-admin
```

Verify `http://localhost:8080/health/ready` returns `Healthy`, then open the frontend on `https://localhost:8443`.

To work on migrations for a provider:

```powershell
# SQL Server
dotnet ef migrations add <Name> --context AppDbContext --project PestiTrackBackend --output-dir Data/Migrations
# PostgreSQL
dotnet ef migrations add <Name> --context PostgresAppDbContext --project PestiTrackBackend --output-dir Data/Migrations/Postgres
```

---

## Part 3: Deploy Free Online — Neon + Render + Vercel (no credit card)

No part of this path asks for a credit card: Neon free, Render free, Vercel Hobby are all free without one.

```
Browser ──HTTPS──> Vercel (React frontend, free)
                        │  /api/* and /health/* rewrites (same origin,
                        │  so the __Host- cookies keep working)
                        ▼
                   Render (ASP.NET Core API, Docker, free)
                        │  DATABASE_URL (postgres://, SSL)
                        ▼
                   Neon (PostgreSQL, free)
```

### Step 1 — Database: Neon (free)
1. Go to [neon.tech](https://neon.tech) and create a free account (no credit card).
2. Create a project (region: Frankfurt is closest to East Africa) and a database, e.g. `pestitrack`.
3. Copy the connection string from the dashboard. It looks like:
   `postgres://user:password@ep-xxx-xxx-pooler.eu-central-1.aws.neon.tech/pestitrack?sslmode=require`
   Paste it exactly as Neon gives it — pooled or direct, either works. (The app converts a pooled `-pooler` host to Neon's direct endpoint automatically, because Npgsql cannot reliably negotiate SSL through Neon's PgBouncer pooler; a single Render service stays well inside Neon's free connection limit.)
4. Keep this string secret. It goes into Render as an env var, never into git.

(Supabase works the same way — create a free project at [supabase.com](https://supabase.com), copy its `postgres://` connection string from Settings → Database, and use it as `DATABASE_URL`.)

### Step 2 — Backend: Render (free)
1. Go to [render.com](https://render.com) and create a free account (no credit card).
2. **New → Blueprint**, connect this GitHub repo (`ellabek-web/PestiTrack_system-`). Render reads `render.yaml` at the repo root and creates the `pestitrack-api` web service (Docker, free plan, Frankfurt).
3. In the service's **Environment** tab, set:
   - `DATABASE_URL` = the Neon connection string from Step 1 (mark it secret).
   - Leave the other vars as the Blueprint set them (`DATABASE_PROVIDER=PostgreSql`, `ReverseProxy__TrustForwardedHeaders=true`, `ReverseProxy__TerminateTlsExternally=true`).
4. Deploy. The container applies Postgres migrations on start, so the first deploy creates all tables. Check the logs for `Applying database migrations...` followed by `Database migrations applied successfully.`
5. Confirm `https://pestitrack-api.onrender.com/health/ready` returns `200 OK` (`Healthy`). Note your exact Render URL — it is `https://<your-service-name>.onrender.com`.
6. Create the first admin once, via **Shell** on the service:
   ```bash
   PESTITRACK_BOOTSTRAP_ADMIN_EMAIL="you@example.com" \
   PESTITRACK_BOOTSTRAP_ADMIN_PASSWORD='ChangeMe_Admin123!' \
   PESTITRACK_BOOTSTRAP_ADMIN_DISPLAY_NAME='System Administrator' \
   dotnet PestiTrackBackend.dll --bootstrap-admin
   ```

> Render free spins down after 15 minutes idle. The first request after that takes ~30–60s while it wakes up — that is normal on the free plan, not an error. The login page will look slow once, then be fast.

### Step 3 — Frontend: Vercel (free)
1. Go to [vercel.com](https://vercel.com) and create a free account (Hobby plan, no credit card).
2. **Add New → Project**, import `ellabek-web/PestiTrack_system-`.
3. Set **Root Directory** to `pestitrack-frontend`, Framework Preset to **Vite** (`npm run build`, output directory `dist`).
4. `pestitrack-frontend/vercel.json` is already filled in with `pestitrack-api.onrender.com` (the service name in `render.yaml`). If Render gave your service a different host, edit both rewrites to match it, commit and push — Vercel redeploys automatically. (Vercel rewrites cannot read env vars, so the host lives in this file.)
   ```json
   { "source": "/api/:path*", "destination": "https://pestitrack-api.onrender.com/api/:path*" }
   ```
   Because Vercel rewrites `/api/*` at the edge under your Vercel HTTPS domain, the browser sends all API requests **same-origin**, keeping `__Host-PestiTrack.Auth` and `__Host-PestiTrack.Csrf` (`SameSite=Strict`) intact.
5. Back in Render, set `Cors__AllowedOrigins` to your Vercel URL (`https://<your-project>.vercel.app`) and redeploy the backend (Manual Deploy → Deploy latest commit).
6. Verify `https://<your-project>.vercel.app/health/ready` returns `Healthy`, then sign in on `https://<your-project>.vercel.app/login` with the admin from Step 2.

### Step 4 — If sign-in fails
- `https://<your-project>.vercel.app/health/ready` unhealthy → the API cannot reach Neon: check `DATABASE_URL` in Render (no trailing spaces, `sslmode=require` present) and that the Neon project is not suspended.
- API healthy but login fails → the Vercel rewrite host is wrong, or `Cors__AllowedOrigins` in Render does not exactly match the Vercel URL (including `https://`, no trailing slash).
- First request very slow → Render free is waking from sleep; wait a minute and retry.

---

## Part 4: Deploy Backend Container + Frontend on Vercel (paid / own database)

Vercel hosts static/frontend apps (`pestitrack-frontend`) and **cannot** run a .NET 10 + SQL Server backend directly. The recommended architecture is:

1. **Database**: Managed SQL Server (Azure SQL Database, AWS RDS SQL Server, or a SQL Server container on your VPS).
2. **Backend (`PestiTrackBackend/Dockerfile`)**: Deploy the Docker image to any HTTPS container host (Render, Railway, Fly.io, Azure Container Apps, DigitalOcean App Platform, or a VPS running Docker).
   - Required environment variables on the container host:
     - `ASPNETCORE_ENVIRONMENT=Production`
     - `ASPNETCORE_URLS=http://+:8080`
     - `ConnectionStrings__DefaultConnection=<your SQL Server connection string>`
     - `ReverseProxy__TrustForwardedHeaders=true`
     - `ReverseProxy__TerminateTlsExternally=true`
   - Migrations run automatically on container start (see `entrypoint.sh`); to run them manually:
     ```bash
     dotnet PestiTrackBackend.dll --migrate
     ```
   - Provision the first admin once with `--bootstrap-admin`.
   - Confirm `https://<YOUR_BACKEND_HOST>/health/ready` returns `200 OK` (`Healthy`).
3. **Frontend (`pestitrack-frontend`) on Vercel**:
   - Edit `pestitrack-frontend/vercel.json` and replace `YOUR_BACKEND_HOST` in both `/api/:path*` and `/health/:path*` rewrites with your deployed HTTPS backend domain (for example `pestitrack-api.onrender.com`).
   - Because Vercel rewrites `/api/*` at the edge under your Vercel HTTPS domain, the browser sends all API requests **same-origin**, keeping `__Host-PestiTrack.Auth` and `__Host-PestiTrack.Csrf` (`SameSite=Strict`) intact.
   - In Vercel (Dashboard or CLI):
     - Set **Root Directory** to `pestitrack-frontend`
     - Framework Preset: **Vite** (`npm run build`, output directory `dist`)
     - Or deploy from CLI:
       ```powershell
       cd pestitrack-frontend
       npx vercel --prod
       ```
   - Verify `https://<your-project>.vercel.app/health/ready` returns `Healthy`, then sign in on `https://<your-project>.vercel.app/login`.
