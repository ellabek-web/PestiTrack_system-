# PestiTrack Deployment Guide (Docker Compose + Vercel)

PestiTrack uses host-only, `Secure`, `HttpOnly` cookies (`__Host-PestiTrack.Auth` and `__Host-PestiTrack.Csrf`) with `SameSite=Lax`/`Strict`. Both deployment paths below preserve **same-origin HTTPS** so cookies and CSRF validation work securely without weakening browser protections.

---

## Part 1: Run Full Stack with Docker Compose

### 1. Configure environment variables
Copy `.env.example` to `.env` and set a strong `SQL_SA_PASSWORD`:

```powershell
Copy-Item .env.example .env
```

### 2. Build and start containers
```powershell
docker compose up -d --build
```

### 3. Apply EF Core database migrations
Run the one-shot migration command against the running database container:

```powershell
docker compose run --rm backend dotnet PestiTrackBackend.dll --migrate
```

### 4. Provision the initial System Administrator
Set `PESTITRACK_BOOTSTRAP_ADMIN_EMAIL`, `PESTITRACK_BOOTSTRAP_ADMIN_PASSWORD`, and `PESTITRACK_BOOTSTRAP_ADMIN_DISPLAY_NAME` (or pass them on the command line) and run `--bootstrap-admin`:

```powershell
docker compose run --rm `
  -e PESTITRACK_BOOTSTRAP_ADMIN_EMAIL="admin@example.com" `
  -e PESTITRACK_BOOTSTRAP_ADMIN_PASSWORD="ChangeMe_Admin123!" `
  -e PESTITRACK_BOOTSTRAP_ADMIN_DISPLAY_NAME="System Administrator" `
  backend dotnet PestiTrackBackend.dll --bootstrap-admin
```

### 5. Verify
- Open `https://localhost:8443` (accept the auto-generated local self-signed certificate, or mount real `tls.crt` / `tls.key` into `/etc/nginx/certs`).
- Health checks: `https://localhost:8443/health/live` and `https://localhost:8443/health/ready`.

---

## Part 2: Deploy Backend Container + Frontend on Vercel

Vercel hosts static/frontend apps (`pestitrack-frontend`) and **cannot** run a .NET 10 + SQL Server backend directly. The recommended architecture is:

1. **Database**: Managed SQL Server (Azure SQL Database, AWS RDS SQL Server, or a SQL Server container on your VPS).
2. **Backend (`PestiTrackBackend/Dockerfile`)**: Deploy the Docker image to any HTTPS container host (Render, Railway, Fly.io, Azure Container Apps, DigitalOcean App Platform, or a VPS running Docker).
   - Required environment variables on the container host:
     - `ASPNETCORE_ENVIRONMENT=Production`
     - `ASPNETCORE_URLS=http://+:8080`
     - `ConnectionStrings__DefaultConnection=<your SQL Server connection string>`
     - `ReverseProxy__TrustForwardedHeaders=true`
     - `ReverseProxy__TerminateTlsExternally=true`
   - Run database migrations during deploy/release:
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
