# TALLY — Invoice Tracker

Full-stack invoice tracking application. Vue 3 frontend, .NET 9 Web API backend, JWT authentication, PostgreSQL database.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) | 9.x | API |
| [Node.js](https://nodejs.org/) | 20+ | UI |
| [Docker + Docker Compose](https://www.docker.com/) | any recent | Full-stack run |

---

## Project structure

```
InvoiceTracker/
├── API/
│   ├── InvoiceTrackerAPI2/         ASP.NET Core 9 Web API
│   │   ├── Controllers/            AuthController, InvoicesController
│   │   ├── Data/                   AppDbContext, Migrations/
│   │   ├── DTOs/                   Validated record DTOs
│   │   │   └── Auth/               LoginDto, RegisterDto, ForgotPasswordDto, ResetPasswordDto
│   │   ├── Mappings/               AutoMapper profile
│   │   ├── Models/                 User, Invoice, LineItem, Enums/
│   │   ├── Repositories/           IInvoiceRepository + implementation
│   │   ├── Services/               IAuthService, IInvoiceService + implementations
│   │   └── appsettings.json        JWT config, connection string, CORS, email
│   └── InvoiceTrackerAPI2.Tests/   xUnit test project
├── UI/
│   └── InvoiceTrackerUI/           Vue 3 + TypeScript + Pinia + Vite
│       └── src/
│           ├── api/                AuthService, InvoiceService (ServiceBase pattern)
│           ├── composables/        useValidation
│           ├── components/
│           ├── models/
│           ├── stores/             authStore, invoiceStore
│           ├── tests/              Vitest test suite
│           └── views/
├── docker-compose.yml              Full stack: postgres + api + frontend
├── Dockerfile                      API container
├── Dockerfile.frontend             Frontend container (nginx)
└── .env                            Environment variables for docker-compose
```

---

## Running with Docker Compose (recommended)

This is the fastest way to run the full stack — PostgreSQL, the API, and the frontend all start together.

### 1. Create your `.env` file

Copy the example and fill in your values:

```bash
cp .env.example .env
```

Open `.env` and set at minimum:

```env
# Required — must be at least 32 characters
JWT_KEY=some_long_secret_key_at_least_32_chars!!

# Optional — leave blank to disable email sending
POSTGRES_PASSWORD=password
EMAIL_SMTP=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=you@gmail.com
EMAIL_PASSWORD=your_app_password
EMAIL_FROM=you@gmail.com
EMAIL_FROM_NAME=Tally
```

### 2. Start everything

```bash
docker compose up --build
```

On first run this builds both containers, starts PostgreSQL, waits for it to be healthy, then starts the API (which runs migrations automatically) and the frontend.

| Service | URL |
|---------|-----|
| Frontend | http://localhost |
| API | http://localhost/api (proxied via nginx) |

### Stop

```bash
docker compose down
```

To also remove the database volume:

```bash
docker compose down -v
```

---

## Running locally (without Docker)

Use this when you want hot-reload for active development.

### Prerequisites

Install the EF Core CLI tool once:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Start PostgreSQL

You still need a running Postgres instance. The easiest way is to start just the database container:

```bash
docker compose up postgres -d
```

Or point the connection string at any existing Postgres server.

### 2. Configure the API

The API reads config from `API/InvoiceTrackerAPI2/appsettings.json`. For local dev, use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to avoid committing credentials:

```bash
cd API/InvoiceTrackerAPI2

dotnet user-secrets set "Jwt:Key" "some_long_secret_key_at_least_32_chars!!"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=invoicetracker;Username=postgres;Password=password"
dotnet user-secrets set "Cors:AllowedOrigins" "http://localhost:5173"
```

### 3. Run the API

```bash
dotnet run --project API/InvoiceTrackerAPI2
```

The API starts on `http://localhost:5086`. Database migrations run automatically on startup.

### 4. Configure the UI

Create `UI/InvoiceTrackerUI/.env.development` (if it doesn't exist):

```env
VITE_API_URL=http://localhost:5086
```

### 5. Run the UI

```bash
cd UI/InvoiceTrackerUI
npm install
npm run dev
```

The UI starts on `http://localhost:5173`.

---

## API reference

### Auth — `/api/auth`

| Method | Endpoint | Body | Auth |
|--------|----------|------|------|
| POST | `/register` | `{ name, email, password }` | — |
| POST | `/login` | `{ email, password }` | — |
| POST | `/forgot-password` | `{ email }` | — |
| POST | `/reset-password` | `{ email, token, newPassword }` | — |

Login/register response:
```json
{ "token": "<jwt>", "user": { "id": 1, "name": "Alice", "email": "alice@example.com" } }
```

All protected requests require:
```
Authorization: Bearer <token>
```

### Invoices — `/api/invoices` _(all require JWT)_

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List invoices — supports `?status=`, `?clientName=`, `?from=`, `?to=`, `?page=`, `?pageSize=` |
| GET | `/{id}` | Get single invoice |
| POST | `/` | Create invoice |
| PUT | `/{id}` | Update invoice |
| PATCH | `/{id}/status` | Update status only |
| DELETE | `/{id}` | Delete invoice |
| POST | `/{id}/send` | Email invoice to recipient |

---

## Running tests

### API tests

```bash
dotnet test API/InvoiceTrackerAPI2.Tests
```

Verbose output:
```bash
dotnet test API/InvoiceTrackerAPI2.Tests --logger "console;verbosity=normal"
```

Filter to a specific class:
```bash
dotnet test API/InvoiceTrackerAPI2.Tests --filter "FullyQualifiedName~AuthServiceTests"
```

### UI tests

```bash
cd UI/InvoiceTrackerUI
npm run test
```

Watch mode:
```bash
npm run test:watch
```

---

## Database migrations

Run these from the repo root when you change EF Core models:

```bash
# Add a new migration
dotnet ef migrations add <Name> --project API/InvoiceTrackerAPI2

# Apply pending migrations
dotnet ef database update --project API/InvoiceTrackerAPI2

# Remove the last migration (if not yet applied)
dotnet ef migrations remove --project API/InvoiceTrackerAPI2
```
