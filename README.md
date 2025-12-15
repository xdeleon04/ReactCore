# ReactCore

Aplicación fullstack con React + ASP.NET Core.

## Catálogo de Productos

- Explorar productos: `GET /shop`
- Detalles del producto: `GET /products/:id`
- Carrito: disponible desde el icono del carrito en el encabezado
- Flujo de pago: `GET /checkout` (requiere inicio de sesión)
- Confirmación de pedido: `GET /orders/:orderNumber`

### Usuarios de demostración (predeterminados)

Si la base de datos está vacía, el backend carga dos usuarios:

- Admin: `admin@example.com` / `Admin123!`
- Usuario: `user@example.com` / `User123!`

## Panel de Administración

- Rutas de la interfaz de administrador: `GET /admin` (Panel), `GET /admin/users`, `GET /admin/products`, `GET /admin/orders`, `GET /admin/reports`, `GET /admin/audit-logs`
- Los endpoints del backend están bajo `/api/admin/*` y requieren `role=admin` en el JWT
- Los endpoints de administrador están limitados por velocidad (política: `admin`) y todas las acciones de administrador se registran en el registro de auditoría de solo adición
- El acceso no autenticado de administrador a `/api/admin/*` devuelve `403` y se registra como `UnauthorizedAccessAttempt`

## Integración de API Externa

Esta función actúa como proxy de OpenWeatherMap a través del backend y representa un widget de clima en el panel de usuario.

### Endpoints

- Clima (autenticado): `GET /api/external/weather?location=Santo Domingo`
- Monitoreo de cuota de administrador (solo admin): `GET /api/admin/api-usage`

### Configuración

- Establece la clave API de OpenWeatherMap mediante variable de entorno: `ExternalApis__OpenWeatherMap__ApiKey`
- Otras configuraciones están en `src/backend/appsettings.json` bajo `ExternalApis:OpenWeatherMap` (TTL, límite de velocidad, disyuntor)

### Comportamiento

- Almacena en caché las respuestas meteorológicas (TTL predeterminado: 30 minutos) con un límite de seguridad máximo de 24 horas
- Rastrea e implementa una cuota por hora; el endpoint de uso de API de administrador emite alertas al 90%+ de uso

### Ejemplo curl

```bash
# Clima (requiere JWT)
curl -H "Authorization: Bearer <token>" "http://localhost:5149/api/external/weather?location=London"

# Estado de cuota de administrador (requiere JWT de administrador)
curl -H "Authorization: Bearer <admin-token>" "http://localhost:5149/api/admin/api-usage"
```

## Autenticación y Seguridad

Este proyecto implementa un sistema de autenticación seguro basado en JWT con las siguientes características:

### Estrategia de Token

- **Token de Acceso**: JWT de corta duración (15 minutos) almacenado en memoria (Contexto de React). Se utiliza para autorización de API mediante el encabezado `Authorization: Bearer`.
- **Token de Actualización**: Token opaco de larga duración (7 días) almacenado en una cookie **HttpOnly, Secure, SameSite=Strict**. Se utiliza para obtener nuevos tokens de acceso de forma transparente.

### Medidas de Seguridad

- **Protección XSS**: Los tokens de acceso no se almacenan en `localStorage` o `sessionStorage`, lo que previene el robo mediante XSS.
- **Protección CSRF**: La cookie del token de actualización utiliza `SameSite=Strict` para evitar ataques CSRF en el endpoint de actualización.
- **Rotación de Token**: Los tokens de actualización se rotan (reemplazan) en cada uso. Los tokens antiguos se invalidan para detectar robos.
- **Límite de Velocidad**: Los intentos de inicio de sesión se limitan a 5 por 15 minutos por correo electrónico para prevenir ataques de fuerza bruta.
- **Seguridad de Contraseña**: Las contraseñas se cifran usando **BCrypt** (factor de trabajo 12) e imponen que sean seguras (8+ caracteres, mayúsculas y minúsculas, número, carácter especial).

### Flujo de Autenticación

1. **Inicio de sesión**: El usuario publica credenciales. El servidor valida y devuelve Token de Acceso (JSON) + Token de Actualización (Cookie).
2. **Acceso**: El cliente envía Token de Acceso en el encabezado.
3. **Caducidad**: Cuando el Token de Acceso caduca (401), el interceptor del cliente llama a `/refresh`.
4. **Actualización**: El servidor valida la cookie, rota el Token de Actualización, devuelve nuevo Token de Acceso + nueva Cookie.
5. **Reintentar**: El cliente reintenta la solicitud original con el nuevo Token de Acceso.
6. **Cerrar sesión**: El cliente llama a `/logout`. El servidor borra la cookie e invalida el token en la BD.

## Configuración

Consulta [SETUP.md](SETUP.md) para obtener instrucciones.
