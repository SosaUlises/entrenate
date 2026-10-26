# Entrenate API: Render + Neon

Esta guía despliega la API como un **Web Service Docker** en Render y usa
Neon PostgreSQL como base de datos de producción. Las credenciales se cargan
exclusivamente como variables de entorno de Render.

## Comportamiento de producción

- El contenedor escucha en `0.0.0.0:$PORT` (`8080` fuera de Render).
- Render termina TLS y la API procesa `X-Forwarded-For` y
  `X-Forwarded-Proto` antes de aplicar HSTS y la redirección HTTPS.
- Swagger sólo está habilitado en `Development`.
- `GET /health` es público, no devuelve configuración ni consulta la base de
  datos. Render puede usarlo como liveness check.
- En `Production`, la API rechaza conexiones PostgreSQL que no exijan SSL/TLS,
  orígenes CORS vacíos y orígenes CORS que no usen HTTPS.
- La API **no ejecuta migrations al iniciar**. El `DatabaseSeeder` existente
  continúa sincronizando de forma idempotente el catálogo después de que el
  esquema haya sido migrado manualmente.

## Conexiones de Neon

Usar dos connection strings distintas:

1. **Runtime:** endpoint pooled de Neon (hostname con `-pooler`) para Render.
2. **Migrations:** endpoint directo de la misma branch, utilizado sólo desde
   una terminal controlada.

Formato Npgsql recomendado para runtime:

```text
Host=ep-<endpoint>-pooler.<region>.aws.neon.tech;Port=5432;Database=<database>;Username=<role>;Password=<password>;SSL Mode=VerifyFull;Channel Binding=Require;Maximum Pool Size=20
```

Formato recomendado para migrations:

```text
Host=ep-<endpoint>.<region>.aws.neon.tech;Port=5432;Database=<database>;Username=<role>;Password=<password>;SSL Mode=VerifyFull;Channel Binding=Require;Pooling=false
```

Copiar los datos desde **Neon > Connection Details**. No guardar ninguna de
estas cadenas en `appsettings*.json`, archivos `.env` versionados ni argumentos
de build de Docker. Si la contraseña contiene caracteres especiales, usar la
connection string Npgsql/.NET entregada por Neon o escapar el valor conforme a
Npgsql.

## Configuración exacta en Render

Crear un **Web Service** conectado al repositorio:

| Campo de Render | Valor |
| --- | --- |
| Runtime | `Docker` |
| Dockerfile Path | `backend/Dockerfile` |
| Docker Build Context Directory | `.` (raíz del repositorio) |
| Docker Command | dejar vacío; se usa el `ENTRYPOINT` |
| Health Check Path | `/health` |
| Region | la más cercana posible a la región de Neon |
| Auto-Deploy | según la política del proyecto |

Render define `PORT` automáticamente (por defecto `10000`). No hace falta
configurarlo manualmente.

### Variables obligatorias

Agregar en **Environment**:

| Variable | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__PostgreConnectionString` | connection string **pooled** de Neon con SSL/TLS |
| `Jwt__Key` | secreto aleatorio de al menos 32 bytes |
| `Jwt__Issuer` | URL pública estable de la API, por ejemplo `https://entrenate-api.onrender.com` |
| `Jwt__Audience` | URL pública estable del frontend |
| `Jwt__ExpirationMinutes` | `60` |
| `Frontend__BaseUrl` | URL pública del frontend, sin `/` final |
| `Cors__AllowedOrigins__0` | dominio de producción de Vercel, por ejemplo `https://entrenate.vercel.app` |
| `AllowedHosts` | hostname de Render y dominios custom separados por `;`, sin esquema |
| `Email__ApiKey` | API key de Brevo, marcada como secreta |
| `Email__FromEmail` | remitente verificado en Brevo |
| `Email__FromName` | `Entrenate` |
| `Groq__ApiKey` | API key de Groq, marcada como secreta |

Para autorizar más dominios estables de Vercel, agregar índices sucesivos:

```text
Cors__AllowedOrigins__1=https://app.entrenate.com
Cors__AllowedOrigins__2=https://otra-url-estable.vercel.app
```

No usar `*` ni autorizar todas las previews de Vercel en producción. Cada
origen debe declararse de forma explícita. `AllowedHosts` acepta, por ejemplo:

```text
entrenate-api.onrender.com;api.entrenate.com
```

`Groq__BaseUrl`, `Groq__Model`, `Groq__MaxOutputTokens`,
`Groq__TimeoutSeconds` y los valores no secretos de email ya tienen defaults en
`appsettings.json`; pueden sobrescribirse con variables del mismo formato si
fuera necesario.

### Persistencia opcional de Data Protection

JWT no depende de archivos locales. Sin embargo, los tokens temporales de
ASP.NET Identity (por ejemplo, recuperación de contraseña) usan Data Protection.
En el filesystem efímero de Render esos tokens se invalidan después de un
redeploy.

Para conservarlos en un servicio pago de una sola instancia:

1. Adjuntar un Persistent Disk con mount path `/app/storage`.
2. Agregar `DataProtection__KeysPath=/app/storage/data-protection-keys`.

El disco de Render está cifrado en reposo. Tener presente que un persistent disk
impide escalar el servicio a varias instancias y deshabilita los deploys sin
downtime. Si no se configura, la API funciona normalmente, pero los tokens
temporales anteriores a un reinicio dejan de ser válidos.

Para generar una clave JWT desde PowerShell sin reutilizar otra credencial:

```powershell
[Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

## Migrations de EF Core en Neon

No configurar `dotnet ef database update` como comando de inicio de la API. Una
migration fallida no debe mezclarse con el arranque ni repetirse en cada
instancia.

Procedimiento recomendado antes del primer deploy y cada cambio de esquema:

1. Crear o restaurar una branch de Neon para ensayar la migration.
2. Usar el endpoint **directo**, no el pooled, y un rol autorizado para DDL.
3. Revisar las migrations pendientes y generar el SQL idempotente.
4. Inspeccionar el SQL.
5. Aplicarlo primero a la branch de ensayo y validar la API.
6. Aplicarlo a producción una sola vez y luego desplegar la nueva imagen.

Desde la raíz del repositorio, en PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__PostgreConnectionString = "<NEON_DIRECT_CONNECTION_STRING>"
$env:Jwt__Key = "<TEMP_VALID_KEY_OF_AT_LEAST_32_BYTES>"
$env:Jwt__Issuer = "https://entrenate-api.onrender.com"
$env:Jwt__Audience = "https://entrenate.vercel.app"
$env:Cors__AllowedOrigins__0 = "https://entrenate.vercel.app"
$env:Email__ApiKey = "<BREVO_API_KEY>"
$env:Frontend__BaseUrl = "https://entrenate.vercel.app"

dotnet ef migrations list `
  --project backend/src/Entrenate.Infrastructure/Entrenate.Infrastructure.csproj `
  --startup-project backend/src/Entrenate.Api/Entrenate.Api.csproj

dotnet ef migrations script --idempotent `
  --project backend/src/Entrenate.Infrastructure/Entrenate.Infrastructure.csproj `
  --startup-project backend/src/Entrenate.Api/Entrenate.Api.csproj `
  --output "$env:TEMP\entrenate-migrations.sql"

dotnet ef database update `
  --project backend/src/Entrenate.Infrastructure/Entrenate.Infrastructure.csproj `
  --startup-project backend/src/Entrenate.Api/Entrenate.Api.csproj `
  --connection $env:ConnectionStrings__PostgreConnectionString
```

`$env:TEMP\entrenate-migrations.sql` es un artefacto temporal para revisión; no
contiene credenciales y queda fuera del repositorio. El comando de
`database update` se ejecuta manualmente sólo después de revisar el script.

Al terminar, limpiar las variables sensibles de la terminal:

```powershell
Remove-Item Env:ConnectionStrings__PostgreConnectionString
Remove-Item Env:Jwt__Key
Remove-Item Env:Email__ApiKey
```

## Validación previa al deploy

```powershell
dotnet build backend/Entrenate.sln --configuration Release
dotnet test backend/Entrenate.sln --configuration Release
docker build --file backend/Dockerfile --tag entrenate-api:local .
```

Una vez desplegado:

```text
GET https://<servicio>.onrender.com/health
```

Debe responder `200`. Swagger no debe estar disponible en producción y una
petición CORS desde un origen no declarado no debe recibir headers de permiso.

## Referencias operativas

- Render Docker: https://render.com/docs/docker
- Render web services y puerto: https://render.com/docs/web-services
- Render health checks: https://render.com/docs/health-checks
- Render persistent disks: https://render.com/docs/disks
- ASP.NET Core detrás de proxy: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- Neon connection strings: https://neon.com/docs/connect/connect-from-any-app
