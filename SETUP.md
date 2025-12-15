# Project Setup Guide

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server (LocalDB or full instance)

## Backend Setup

1. Navigate to `src/backend`:

   ```bash
   cd src/backend
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Update database (apply migrations):

   ```bash
   dotnet ef database update
   ```

   Note: the backend also applies migrations on startup in development, but `dotnet ef database update` is the most explicit way to ensure your DB schema is up-to-date.
4. Run the application:

   ```bash
   dotnet run
   ```

   The backend will start on `http://localhost:5149` (http) by default.

## External API (OpenWeatherMap) Configuration

The weather widget uses OpenWeatherMap via the backend proxy. Configure the API key using standard .NET configuration overrides.

### Recommended (environment variable)

Set:

```env
ExternalApis__OpenWeatherMap__ApiKey=your_openweathermap_api_key
```

You can set this in your shell environment, a hosting provider, or user secrets.

### Alternative (appsettings.Development.json)

Update `src/backend/appsettings.Development.json` under:

`ExternalApis:OpenWeatherMap:ApiKey`

## Seed Data

If the database is empty, the backend seeds:

- Users: `admin@example.com` / `Admin123!`, `user@example.com` / `User123!`
- A small set of sample products (electronics + sports)

## Frontend Setup

1. Navigate to `src/frontend`:

   ```bash
   cd src/frontend
   ```

2. Install dependencies:

   ```bash
   npm install
   ```

3. Run the development server:

   ```bash
   npm run dev
   ```

   The frontend will start on `http://localhost:5173` (or similar).

### Configure API Base URL (optional)

Frontend API calls default to `http://localhost:5149/api`. To override:

1. Create `src/frontend/.env.local`
2. Add:

   ```env
   VITE_API_BASE_URL=http://localhost:5149/api
   ```

## Testing

### Backend Tests

```bash
dotnet test tests/backend/ReactCore.Backend.Tests/ReactCore.Backend.Tests.csproj
```

### Frontend Tests

```bash
cd src/frontend
npm test
```

## E2E Tests (Playwright)

The Playwright test runner lives in `tests/e2e`.

### Prerequisites (E2E)

1. Ensure the frontend can reach the backend:

   - Default is `http://localhost:5149/api` (see `VITE_API_BASE_URL` above)
   - CORS allows `http://localhost:5173` by default via `Cors:FrontendOrigin`

The E2E config will start both the backend (port 5149) and the frontend dev server (port 5173) automatically.

### Run the tests

```bash
cd tests/e2e
npm install
npx playwright install
npx playwright test
```

To run only the weather widget test:

```bash
cd tests/e2e
npx playwright test external_api_weather.spec.ts
```
