# Guía de Configuración del Proyecto

## Requisitos Previos

- .NET 8 SDK
- Node.js 18+
- SQL Server (LocalDB o instancia completa)

## Configuración del Backend

1. Navega a `src/backend`:

   ```bash
   cd src/backend
   ```
2. Restaura las dependencias:

   ```bash
   dotnet restore
   ```
3. Actualiza la base de datos (aplica migraciones):

   ```bash
   dotnet ef database update
   ```

   Nota: el backend también aplica migraciones al iniciar en modo desarrollo, pero `dotnet ef database update` es la forma más explícita de asegurar que tu esquema de BD esté actualizado.
4. Ejecuta la aplicación:

   ```bash
   dotnet run
   ```

   El backend iniciará en `http://localhost:5149` (http) por defecto.

## Configuración de API Externa (OpenWeatherMap)

El widget de clima utiliza OpenWeatherMap a través del proxy del backend. Configura la clave API usando anulaciones de configuración estándar de .NET.

### Recomendado (variable de entorno)

Establece:

```env
ExternalApis__OpenWeatherMap__ApiKey=tu_clave_openweathermap_api
```

Puedes configurar esto en tu entorno de shell, proveedor de alojamiento o secretos de usuario.

### Alternativa (appsettings.Development.json)

Actualiza `src/backend/appsettings.Development.json` en:

`ExternalApis:OpenWeatherMap:ApiKey`

## Datos de Inicialización

Si la base de datos está vacía, el backend genera:

- Usuarios: `admin@example.com` / `Admin123!`, `user@example.com` / `User123!`
- Un pequeño conjunto de productos de ejemplo (electrónica + deportes)

## Configuración del Frontend

1. Navega a `src/frontend`:

   ```bash
   cd src/frontend
   ```
2. Instala las dependencias:

   ```bash
   npm install
   ```
3. Ejecuta el servidor de desarrollo:

   ```bash
   npm run dev
   ```

   El frontend iniciará en `http://localhost:5173` (o similar).

### Configura la URL Base de la API (opcional)

Las llamadas a la API del frontend usan por defecto `http://localhost:5149/api`. Para anular:

1. Crea `src/frontend/.env.local`
2. Añade:

   ```env
   VITE_API_BASE_URL=http://localhost:5149/api
   ```

## Pruebas

### Pruebas del Backend

```bash
dotnet test tests/backend/ReactCore.Backend.Tests/ReactCore.Backend.Tests.csproj
```

### Pruebas del Frontend

```bash
cd src/frontend
npm test
```

## Pruebas E2E (Playwright)

El ejecutor de pruebas Playwright se encuentra en `tests/e2e`.

### Requisitos Previos (E2E)

1. Asegúrate de que el frontend pueda alcanzar el backend:

   - El valor por defecto es `http://localhost:5149/api` (ver `VITE_API_BASE_URL` arriba)
   - CORS permite `http://localhost:5173` por defecto a través de `Cors:FrontendOrigin`

La configuración de E2E iniciará automáticamente tanto el backend (puerto 5149) como el servidor de desarrollo del frontend (puerto 5173).

### Ejecuta las pruebas

```bash
cd tests/e2e
npm install
npx playwright install
npx playwright test
```

Para ejecutar solo la prueba del widget de clima:

```bash
cd tests/e2e
npx playwright test external_api_weather.spec.ts
```
