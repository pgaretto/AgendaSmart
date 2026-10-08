# Smart-Agenda

Calendario que registra gastos automáticamente a partir de una frase en lenguaje natural. Los eventos del día y el presupuesto mensual disponible conviven en una sola pantalla.

> *"El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil"* → evento **Dentista** el martes a las 16:00 + gasto de **$25.000** en **Salud**, listo para confirmar.

El detalle de requisitos y criterios de aceptación está en [`PRD.md`](PRD.md). Las reglas para trabajar en el repo (incluido con agentes de IA) están en [`AGENTS.md`](AGENTS.md).

## Qué hace

- **Entrada única por texto:** el usuario escribe una frase libre; la IA (Claude Haiku) extrae evento (título, fecha, hora) y/o gasto (monto, categoría).
- **Confirmación obligatoria:** nada se guarda sin pasar por el modal "¿Está todo bien?", con todos los campos editables.
- **Calendario** mensual y semanal con los eventos de cada día.
- **Presupuesto mensual** editable, con el disponible restante (presupuesto − gastos del mes).
- **Cuentas individuales:** registro e inicio de sesión; cada usuario ve solo sus datos.
- **Sin conexión:** los envíos fallidos por falta de señal se guardan en el navegador y se reintentan solos (hasta 5 veces). Si no se logra, quedan pendientes de reintento manual con aviso.

Categorías de gasto: Comida, Transporte, Salidas, Salud y Otros (esta última también para gastos ambiguos).

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | React 19 + Vite, Node 24 LTS |
| Backend | ASP.NET, .NET 10 |
| Base de datos | SQL Server (LocalDB en desarrollo) |
| IA | Anthropic, Claude Haiku (`claude-haiku-5-5`) |
| Auth | JWT + BCrypt |

## Estructura

```
backend/         API ASP.NET (controllers, servicios, EF Core, migraciones)
backend.Tests/   Tests de la API (xUnit) y evaluación de la IA (Eval/)
frontend/        App React (componentes, hooks, servicios)
PRD.md           Requisitos y criterios de aceptación
```

## Puesta en marcha

### Requisitos previos

- Node 24 LTS
- .NET 10 SDK
- SQL Server o LocalDB
- Una API key de Anthropic (<https://console.anthropic.com>)
- Herramienta de migraciones: `dotnet tool install --global dotnet-ef`

### 1. Backend

Los secretos **no** van en el repo: se guardan con user-secrets.

```bash
cd backend
dotnet restore

# Clave para firmar los JWT (largo y aleatorio; 32+ caracteres)
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-aleatoria-de-32+-caracteres"

# API key de Anthropic (RF-05, RF-06)
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."

# Crear la base de datos (usa la cadena de conexión de appsettings.Development.json)
dotnet ef database update

dotnet run
```

La API queda en <http://localhost:5222>. Si usás otra instancia de SQL Server, cambiá `ConnectionStrings:DefaultConnection` en `appsettings.Development.json`.

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

La app queda en <http://localhost:5173>. Por defecto apunta a `http://localhost:5222`; para cambiarlo creá `frontend/.env.development` con:

```
VITE_API_URL=http://localhost:5222
```

Los orígenes permitidos por CORS se configuran en `Cors:AllowedOrigins` (por defecto, puertos 5173 y 5174).

## Tests

```bash
# Backend (usa base en memoria y una IA simulada: no necesita SQL Server ni API key)
cd backend.Tests && dotnet test

# Frontend
cd frontend && npm test
```

### Evaluación de la IA (RNF-06)

`backend.Tests/Eval/phrases.json` es el dataset de validación: 63 frases coloquiales con monto y categoría esperados. El test exige al menos **90%** de coincidencia exacta; la última medición dio **98,4%** (62/63).

Hace llamadas reales a Claude (cuestan unos centavos), así que se saltea salvo que definas la key:

```bash
ANTHROPIC_EVAL_KEY="sk-ant-..." dotnet test --filter ExtractionAccuracyEval
```

Para agregar casos, sumá entradas al JSON con `text`, `amount` y `category` (o `null` si la frase no tiene gasto). Escribí la expectativa **antes** de ver qué responde la IA.

## API

Todo salvo registro y login requiere `Authorization: Bearer <token>`.

| Método y ruta | Descripción |
|---|---|
| `POST /api/auth/register` | Crea la cuenta (contraseña de al menos 8 caracteres). |
| `POST /api/auth/login` | Devuelve el JWT (válido 60 minutos). |
| `GET /api/auth/me` | Usuario autenticado. |
| `GET /api/events?from=&to=` | Eventos del usuario en un rango. |
| `GET /api/expenses?from=&to=` | Gastos del usuario en un rango. |
| `GET/POST/PUT /api/budget` | Presupuesto del mes (`year`, `month`, `amount`) con gastado y disponible. |
| `POST /api/parse` | Interpreta una frase y devuelve la propuesta `{ event, expense }`. **No guarda nada.** |
| `POST /api/entries` | Guarda lo ya confirmado por el usuario, todo o nada. |

## Configuración relevante

| Clave | Por defecto | Para qué |
|---|---|---|
| `Anthropic:Model` | `claude-haiku-5-5` | Modelo de la extracción. |
| `Anthropic:TimeoutSeconds` | `4` | Corta la llamada a la IA para cumplir RNF-05 (< 5 s). |
| `Anthropic:MaxRetries` | `0` | Sin reintentos automáticos del SDK. |
| `RateLimit:AiPermitPerMinute` | `10` | Consultas a la IA por usuario por minuto (`429` al excederse). |
| `Jwt:ExpiresInMinutes` | `60` | Duración de la sesión. |

## Decisiones y límites a tener en cuenta

- **Aislamiento de datos (RNF-03):** todas las consultas filtran por el usuario del token; el dueño de un registro nunca se toma del cuerpo de la petición. También las colas locales del navegador están separadas por usuario.
- **Sesión vencida:** al vencer el token se vuelve al login y lo que estaba pendiente de envío se conserva y se reenvía solo al volver a entrar. No hay renovación automática del token.
- **Límite de uso de la IA:** los contadores viven en la memoria del servidor; se reinician con él y no se comparten entre instancias.
- **Fecha del gasto:** viene del modal (por defecto, la del evento o la de hoy). Es la que define en qué mes descuenta del presupuesto.
- **Horario por defecto:** si la frase no trae hora, el evento queda a las 09:00 y el usuario lo corrige en el modal.

## Fuera de alcance (v1)

Integración con Google/Apple Calendar, vinculación con bancos o billeteras, multi-moneda y entrada por voz/audio.
