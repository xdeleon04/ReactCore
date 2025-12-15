# ReactCore Frontend

Frontend de React + TypeScript + Vite (rolldown-vite) para el repositorio fullstack ReactCore.

## Inicio Rápido

Desde esta carpeta:

- Instalar: `npm install`
- Servidor de desarrollo: `npm run dev`
- Lint: `npm run lint`
- Compilación de producción: `npm run build`
- Vista previa de compilación: `npm run preview`

## Enrutamiento

Las rutas se definen en `src/App.tsx` y `src/routes/*`.

- Públicas: `/login`, `/shop`, `/products/:id`
- Autenticadas: `/dashboard`, `/checkout`, `/orders/:orderNumber`
- Admin (con control de rol): `/admin/*` (ver `src/routes/AdminRoutes.tsx`)

## Diseño y Navegación

El shell de la aplicación es `src/components/layouts/MainLayout.tsx` con:

- `src/components/features/Header.tsx` (navegación superior)
- `src/components/features/Sidebar.tsx` (navegación izquierda en escritorio)

Los enlaces de navegación de administrador aparecen cuando el usuario conectado es administrador. La barra lateral incluye una sección Admin dedicada con enlaces a:

- `/admin/dashboard`
- `/admin/users`
- `/admin/products`
- `/admin/orders`
- `/admin/reports`
- `/admin/api-usage`
- `/admin/audit-logs`

## Primitivos de UI (estilo shadcn)

Los primitivos reutilizables de Tailwind se encuentran en `src/components/ui/*`.

Ver `src/components/ui/README.md` para APIs de componentes y ejemplos.

## Notificaciones (Toasts)

Utiliza el wrapper de notificaciones de la aplicación (no importes `react-hot-toast` directamente en código de características):

```tsx
import { useToast } from '@/hooks/useToast'

const toast = useToast()
toast.success({ title: 'Guardado' })
toast.error({ title: 'Error', description: 'Por favor, inténtalo de nuevo.' })
```

`ToastProvider` está montado en la raíz de la aplicación.

