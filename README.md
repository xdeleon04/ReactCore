# ReactCore

Fullstack application with React + ASP.NET Core.

## Product Catalog (Feature 002)

- Browse products: `GET /shop`
- Product details: `GET /products/:id`
- Cart drawer: available from the header cart badge
- Checkout flow: `GET /checkout` (requires login)
- Order confirmation: `GET /orders/:orderNumber`

### Demo users (seeded)

If the database is empty, the backend seeds two users:

- Admin: `admin@example.com` / `Admin123!`
- User: `user@example.com` / `User123!`

## Admin Dashboard (Feature 003)

- Admin UI routes: `GET /admin` (Dashboard), `GET /admin/users`, `GET /admin/products`, `GET /admin/orders`, `GET /admin/reports`, `GET /admin/audit-logs`
- Backend endpoints are under `/api/admin/*` and require `role=admin` in the JWT
- Admin endpoints are rate limited (policy: `admin`) and all admin actions are recorded in the append-only audit log
- Authenticated non-admin access to `/api/admin/*` returns `403` and is audited as `UnauthorizedAccessAttempt`

## External API Integration (Feature 004)

This feature proxies OpenWeatherMap through the backend and renders a weather widget on the user dashboard.

### Endpoints

- Weather (authenticated): `GET /api/external/weather?location=London`
- Admin quota monitoring (admin-only): `GET /api/admin/api-usage`

### Configuration

- Set the OpenWeatherMap API key via environment variable: `ExternalApis__OpenWeatherMap__ApiKey`
- Other settings are in `src/backend/appsettings.json` under `ExternalApis:OpenWeatherMap` (TTL, rate limit, circuit breaker)

### Behavior

- Caches weather responses (default TTL: 30 minutes) with a 24-hour max-stale safety boundary
- Tracks and enforces an hourly quota; admin API usage endpoint emits alerts at 90%+ usage

### Example curl

```bash
# Weather (requires JWT)
curl -H "Authorization: Bearer <token>" "http://localhost:5149/api/external/weather?location=London"

# Admin quota status (requires admin JWT)
curl -H "Authorization: Bearer <admin-token>" "http://localhost:5149/api/admin/api-usage"
```

## Authentication & Security

This project implements a secure JWT-based authentication system with the following features:

### Token Strategy
- **Access Token**: Short-lived (15 minutes) JWT stored in memory (React Context). Used for API authorization via `Authorization: Bearer` header.
- **Refresh Token**: Long-lived (7 days) opaque token stored in an **HttpOnly, Secure, SameSite=Strict** cookie. Used to obtain new access tokens transparently.

### Security Measures
- **XSS Protection**: Access tokens are not stored in `localStorage` or `sessionStorage`, preventing theft via XSS.
- **CSRF Protection**: Refresh token cookie uses `SameSite=Strict` to prevent CSRF attacks on the refresh endpoint.
- **Token Rotation**: Refresh tokens are rotated (replaced) on every use. Old tokens are invalidated to detect theft.
- **Rate Limiting**: Login attempts are limited to 5 per 15 minutes per email to prevent brute-force attacks.
- **Password Security**: Passwords are hashed using **BCrypt** (work factor 12) and enforced to be strong (8+ chars, mixed case, number, special char).

### Auth Flow
1.  **Login**: User posts credentials. Server validates and returns Access Token (JSON) + Refresh Token (Cookie).
2.  **Access**: Client sends Access Token in header.
3.  **Expiry**: When Access Token expires (401), client interceptor calls `/refresh`.
4.  **Refresh**: Server validates cookie, rotates Refresh Token, returns new Access Token + new Cookie.
5.  **Retry**: Client retries original request with new Access Token.
6.  **Logout**: Client calls `/logout`. Server clears cookie and revokes token in DB.

## Setup

See [SETUP.md](SETUP.md) for instructions.
