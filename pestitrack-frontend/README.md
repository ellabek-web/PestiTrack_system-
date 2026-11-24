# PestiTrack frontend

React, TypeScript, Vite, Material UI, React Router, and TanStack Query client for the
PestiTrack ASP.NET Core API.

## Development

1. Start `PestiTrackBackend` with its HTTPS launch profile (`https://localhost:7296`).
2. Trust the ASP.NET Core development certificate if necessary:
   `dotnet dev-certs https --trust`.
3. In this directory, run `npm install` and `npm run dev`.
4. Open `http://localhost:5173`.

Vite proxies `/api` to the HTTPS backend. This keeps the cookie-based session on one
browser origin and avoids requiring backend CORS configuration during development.

## Security integration

The API client always sends credentials. Before each POST, PUT, PATCH, or DELETE it
fetches `/api/auth/csrf` and sends the returned token as `X-CSRF-TOKEN`.

## Commands

- `npm run dev` — start the development server
- `npm run build` — type-check and build
- `npm run lint` — run Oxlint
- `npm run preview` — preview the production build
