# Entrenate API: Render + Neon

Esta guía despliega la API como un **Web Service Docker** en Render y usa
Neon PostgreSQL como base de datos de producción. Las credenciales se cargan
exclusivamente como variables de entorno de Render.

## Estado actual

- Backend: `https://entrenate.onrender.com`
- Frontend: `https://entrenate-three.vercel.app`
- Base de datos: PostgreSQL en Neon.
- EF Core: las 9 migrations existentes ya están aplicadas en producción.
- Catálogo inicial: 37 ejercicios y 24 equipamientos.
- Health check: `GET https://entrenate.onrender.com/health` responde `200`
  con `Healthy`.

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

### Variables obligatorias y secretos

Agregar en **Environment**:

| Variable | Valor | Tipo |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Configuración |
| `ConnectionStrings__PostgreConnectionString` | connection string **pooled** de Neon con SSL/TLS | Secreto |
| `Jwt__Key` | valor aleatorio de al menos 32 bytes | Secreto |
| `Jwt__Issuer` | `https://entrenate.onrender.com` | Configuración |
| `Jwt__Audience` | `https://entrenate-three.vercel.app` | Configuración |
| `Frontend__BaseUrl` | `https://entrenate-three.vercel.app` | Configuración |
| `Cors__AllowedOrigins__0` | `https://entrenate-three.vercel.app` | Configuración |
| `AllowedHosts` | `entrenate.onrender.com` | Configuración |
| `Email__ApiKey` | API key de Brevo | Secreto |
| `Groq__ApiKey` | API key de Groq | Secreto |

Marcar como secretos en Render la connection string, `Jwt__Key`,
`Email__ApiKey` y `Groq__ApiKey`. No guardar sus valores en el repositorio.

### Valores ya definidos en `appsettings.json`

Los siguientes valores no necesitan repetirse en Render mientras sigan siendo
correctos para producción:

| Variable | Default actual |
| --- | --- |
| `Jwt__ExpirationMinutes` | `60` |
| `Email__FromEmail` | remitente configurado en el repositorio; debe estar verificado en Brevo |
| `Email__FromName` | `Entrenate` |
| `Groq__BaseUrl` | `https://api.groq.com/openai/v1/` |
| `Groq__Model` | `openai/gpt-oss-120b` |
| `Groq__MaxOutputTokens` | `400` |
| `Groq__TimeoutSeconds` | `30` |

`Frontend__BaseUrl` sí debe sobrescribirse en Render porque su default compartido
es local. CORS no tiene un default compartido: `http://localhost:3000` está
exclusivamente en `appsettings.Development.json` y no se carga en Production.

Para autorizar más dominios estables de Vercel, agregar índices sucesivos:

```text
Cors__AllowedOrigins__1=<OTRO_ORIGEN_HTTPS_ESTABLE>
Cors__AllowedOrigins__2=<TERCER_ORIGEN_HTTPS_ESTABLE>
```

No usar `*` ni autorizar todas las previews de Vercel en producción. Cada
origen debe declararse de forma explícita. Para el despliegue actual,
`AllowedHosts` es:

```text
entrenate.onrender.com
```

No agregar `http://localhost:3000` a Render. En Production, el backend exige al
menos un origen, valida que todos usen HTTPS y no habilita wildcards.

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
$keyBytes = New-Object byte[] 64
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $rng.GetBytes($keyBytes)
    [Convert]::ToBase64String($keyBytes)
}
finally {
    $rng.Dispose()
}
```

## Migrations de EF Core en Neon

No configurar `dotnet ef database update` como comando de inicio de la API. Una
migration fallida no debe mezclarse con el arranque ni repetirse en cada
instancia.

Las 9 migrations actuales ya están registradas en `__EFMigrationsHistory` de
producción. No volver a aplicarlas manualmente: EF Core sólo debe ejecutar una
actualización cuando `dotnet ef migrations list` muestre migrations nuevas como
pendientes.

Procedimiento para futuras migrations:

1. Crear o restaurar una branch de Neon para ensayar la migration.
2. Usar el endpoint **directo**, no el pooled, y un rol autorizado para DDL.
3. Ejecutar `migrations list`. Si no hay ninguna pendiente, detener el
   procedimiento sin ejecutar `database update`.
4. Si hay migrations pendientes, generar el SQL idempotente correspondiente.
5. Inspeccionar el SQL y descartar operaciones destructivas inesperadas.
6. Aplicarlo primero a la branch de ensayo y validar la API.
7. Aplicarlo a producción una sola vez, verificar `__EFMigrationsHistory` y
   luego desplegar la nueva imagen.

Desde la raíz del repositorio, en PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__PostgreConnectionString = "<NEON_DIRECT_CONNECTION_STRING>"
$env:Jwt__Key = "<TEMP_VALID_KEY_OF_AT_LEAST_32_BYTES>"
$env:Jwt__Issuer = "https://entrenate.onrender.com"
$env:Jwt__Audience = "https://entrenate-three.vercel.app"
$env:Cors__AllowedOrigins__0 = "https://entrenate-three.vercel.app"
$env:Email__ApiKey = "<BREVO_API_KEY>"
$env:Frontend__BaseUrl = "https://entrenate-three.vercel.app"

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

## Despliegue del frontend en Vercel

Crear el proyecto de Vercel desde el mismo repositorio y usar esta configuración:

| Campo de Vercel | Valor |
| --- | --- |
| Framework Preset | `Next.js` |
| Root Directory | `frontend` |
| Install Command | automático (`npm install`) |
| Build Command | automático (`npm run build`) |
| Output Directory | default de Next.js |

El frontend lee la URL del backend desde `NEXT_PUBLIC_API_URL`, definida en
`frontend/src/lib/env.ts`. Configurar en Vercel, al menos para Production:

```text
NEXT_PUBLIC_API_URL=https://entrenate.onrender.com
```

El valor no debe incluir `/api` ni una barra final: el cliente centralizado
agrega la ruta de cada endpoint. No guardar secretos en variables
`NEXT_PUBLIC_*`, porque sus valores quedan disponibles en el navegador.

Si se habilitan deployments Preview que deban consumir la API, su origen exacto
también debe declararse explícitamente en Render con otro índice de
`Cors__AllowedOrigins`. No autorizar patrones ni todas las previews mediante
wildcards.

## Validación previa al deploy

```powershell
dotnet build backend/Entrenate.sln --configuration Release
dotnet test backend/Entrenate.sln --configuration Release
docker build --file backend/Dockerfile --tag entrenate-api:local .
```

Una vez desplegado:

```text
GET https://entrenate.onrender.com/health
```

Debe responder `200` con `Healthy`. Swagger no debe estar disponible en
producción y una petición CORS desde un origen no declarado no debe recibir
headers de permiso.

## Referencias operativas

- Render Docker: https://render.com/docs/docker
- Render web services y puerto: https://render.com/docs/web-services
- Render health checks: https://render.com/docs/health-checks
- Render persistent disks: https://render.com/docs/disks
- ASP.NET Core detrás de proxy: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- Neon connection strings: https://neon.com/docs/connect/connect-from-any-app
