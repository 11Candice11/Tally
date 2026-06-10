# InvoiceTrackerAPI2 — CLAUDE.md

## Stack
- .NET 9, ASP.NET Core Web API
- PostgreSQL via EF Core (Npgsql), migrations in `Migrations/`
- Redis (StackExchange.Redis) for JWT revocation and output caching
- JWT Bearer auth (symmetric key), access + refresh token pattern
- AutoMapper (`InvoiceMappingProfile`) for entity → DTO mapping
- QuestPDF for PDF generation (`InvoicePdfService`)
- MailKit / SMTP for email (`EmailService`)
- Serilog with compact JSON output to console and rolling daily files at `/app/logs/`
- Background job: `OverdueInvoiceJob` (IHostedService) marks overdue invoices

## Project layout
```
Controllers/      AuthController, InvoicesController, ClientsController
DTOs/             Request/response DTOs (flat + nested under Auth/)
Models/           Entity classes (User, Invoice, LineItem, Client, RefreshToken, InvoiceCounter)
Models/Enums/     InvoiceStatus enum
Repositories/     Concrete repos + Interfaces/ subfolder
Services/         Business logic + Interfaces/ subfolder
Data/             AppDbContext
Mappings/         InvoiceMappingProfile (AutoMapper)
Migrations/       EF Core migration files
```

## Auth flow
- `POST /api/auth/register` / `login` → returns `{ token, refreshToken, user }`
- Access tokens are short-lived JWTs (JTI claim used for revocation in Redis)
- Refresh tokens are stored in the `RefreshTokens` table; single-use (rotated on each refresh)
- `POST /api/auth/refresh` exchanges a refresh token for a new token pair
- `POST /api/auth/logout` revokes the JTI in Redis and invalidates the refresh token
- `POST /api/auth/admin/create` is `[Authorize(Roles = "Admin")]`
- Password reset: forgot-password stores a token on the User entity; reset-password validates it

## Authorization
- All invoice and client endpoints require `[Authorize]` (valid JWT)
- Controllers extract `userId` from `ClaimTypes.NameIdentifier` or `"sub"` claim via `TryGetUserId()`
- Admin endpoints use `[Authorize(Roles = "Admin")]`
- Every data access is scoped to `userId` — users only see their own data

## Output caching
- `GET /api/invoices/summary` uses `SummaryCache` policy (30s TTL, varies by query string and Authorization header)
- Cache is tagged `"summary"` for targeted invalidation

## Response conventions
- Success: typed DTO or `{ message }` object
- Not found: `NotFound()` (no body)
- Conflict / duplicate: `Conflict(new { message })`
- Auth failure: `Unauthorized(new { message })`
- Invalid state transition: `UnprocessableEntity(new { message })`
- Unhandled exceptions: global handler returns `500 { message: "An unexpected error occurred." }` — no stack traces
- Enum values serialized as strings (`JsonStringEnumConverter` registered globally)

## Key config (appsettings.json / User Secrets / env vars)
```
Jwt:Key            symmetric signing key (≥32 bytes required)
Jwt:Issuer         "InvoiceTrackerAPI"
Jwt:Audience       "InvoiceTrackerUI"
ConnectionStrings:Default   PostgreSQL connection string
ConnectionStrings:Redis      Redis connection string
Email:Smtp / Port / Username / Password / From / FromName
```
Sensitive values go in User Secrets (dev) or environment variables (prod). Never commit real values.

## Database
- EF Core with Npgsql; `AppDbContext` has `Users`, `Invoices`, `LineItems`, `Clients`, `RefreshTokens`, `InvoiceCounters`
- Migrations run automatically on startup via `Database.Migrate()`
- Add a migration: `dotnet ef migrations add <Name> --project API/InvoiceTrackerAPI2`
- The `invoicetracker.db` file in the project root is a leftover SQLite file — the real DB is PostgreSQL

## Invoice numbering
- `SequentialInvoiceNumberAllocator` uses the `InvoiceCounters` table for per-user sequential numbers
- Unique index on `(UserId, InvoiceNumber)` — allocator must be called inside a transaction

## Running locally
```bash
# From repo root
docker-compose up         # starts API + PostgreSQL + Redis
```
API runs on port 8080 inside Docker, proxied through nginx.

## Tests (InvoiceTrackerAPI2.Tests)
- xUnit + Moq
- Service tests use `TestDbContext.Create()` — EF Core InMemory database (unique name per test for isolation)
- Controller tests mock the service layer via Moq
- No mocking the database in service tests — real EF InMemory provider
- Run: `dotnet test` from repo root or `API/InvoiceTrackerAPI2.Tests/`
- Pattern: `CreateService()` factory method wires up real dependencies with in-memory DB

## TODOs in codebase
- RabbitMQ for async invoice processing (noted in Program.cs)
- Token storage migration from localStorage to HttpOnly cookies (noted in UI)
