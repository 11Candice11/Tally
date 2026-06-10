# InvoiceTrackerUI — CLAUDE.md

## Stack
- Vue 3 (Composition API, `<script setup>`)
- TypeScript (strict mode)
- Vite (build tool + dev server)
- Pinia (state management)
- Vue Router 4
- Axios (HTTP client)
- Chart.js + vue-chartjs (reports/charts)
- Vitest + @vue/test-utils + happy-dom (unit tests)

## Project layout
```
src/
  api/           HTTP layer — http.ts (axios instance), authApi.ts, invoiceApi.ts,
                 AuthService.ts, InvoiceService.ts, ClientService.ts, ServiceBase.ts
  components/    Reusable: AppSidebar, InvoiceForm, InvoiceTable, InvoiceStatusBadge,
                 InvoiceSummaryCards, ConfirmDialog
  composables/   useInvoices.ts, useValidation.ts
  models/        TypeScript types — invoice.ts, client.ts
  router/        index.ts (Vue Router config)
  stores/        authStore.ts, invoiceStore.ts, clientStore.ts (Pinia)
  views/         Page-level components: Dashboard, InvoiceList, InvoiceDetail,
                 InvoiceForm, InvoicePdf, Login, Clients, Reports
  tests/         api/, components/, models/, stores/ — mirrors src structure
```

## API communication
- `src/api/http.ts` creates an axios instance with `baseURL: import.meta.env.VITE_API_URL`
- Request interceptor attaches `Authorization: Bearer <token>` from `localStorage`
- Response interceptor catches 401s, clears localStorage, redirects to `/login`
- `extractApiError(err)` in http.ts normalises Axios errors into a user-facing string —
  handles ASP.NET Core `ValidationProblemDetails` (`errors` object), plain `{ message }`, and network errors
- Use `extractApiError` in every catch block that shows a UI error message

## Auth
- Tokens stored in `localStorage` under keys `jwt_token`, `jwt_refresh`, `jwt_user`, `jwt_expiry`
- `authStore` (Pinia) manages session state, parses JWT expiry from payload, and schedules a
  silent refresh 1 minute before expiry via `setTimeout`
- On page load, `restoreSession()` re-arms the refresh timer or triggers silent refresh if already expired
- `isAuthenticated` computed: `!!token && (expiresAt === null || Date.now() < expiresAt)`
- **Known issue / TODO**: tokens in localStorage are vulnerable to XSS — plan is to migrate to HttpOnly cookies

## Routing
- All routes except `/login` (meta: `{ public: true }`) require authentication
- Navigation guard in `router/index.ts`: unauthenticated → redirect to Login; already authenticated on Login → redirect to Dashboard
- Routes: `/` Dashboard, `/invoices`, `/invoices/new`, `/invoices/:id`, `/invoices/:id/edit`, `/invoices/:id/pdf`, `/clients`, `/reports`

## State (Pinia stores)
- `authStore` — token, refreshToken, user, isAuthenticated, login/logout/register/forgotPassword
- `invoiceStore` — invoice list, CRUD operations, filters
- `clientStore` — client list, CRUD operations

## Models / types (`src/models/invoice.ts`)
- `InvoiceStatus` enum: `Draft | Sent | Paid | Overdue | Cancelled`
- `Invoice` interface has `vatRate` (decimal, e.g. 0.15), `discount` and `lateFee` (absolute amounts)
- Helper functions: `invoiceSubtotal()`, `invoiceVat()`, `invoiceTotal()` — use these, don't recalculate inline
- `CreateInvoiceDto = Omit<Invoice, 'id' | 'activity'>`

## Environment variables
```
VITE_API_URL    Base URL for the API (e.g. http://localhost:5000 in dev, / in prod via nginx)
```
See `.env.example` for the template. Dev values are in `.env.development`.

## Running locally
```bash
cd UI/InvoiceTrackerUI
npm install
npm run dev          # Vite dev server, hot reload
```
In production, nginx serves the built static files and proxies `/api` to the API container.

## Tests
- Vitest + @vue/test-utils, runs in happy-dom environment
- `npm test` runs all tests once; `npm run test:watch` for watch mode
- Test files live in `src/tests/`, mirroring the source structure
- API service tests: mock `http.ts` with `vi.mock('../../api/http', ...)`
- Store tests: use `setActivePinia(createPinia())` + `localStorage.clear()` in `beforeEach`
- Component tests: mount with `@vue/test-utils`, mock Pinia stores via `@pinia/testing`
- Always call `vi.clearAllMocks()` in `beforeEach`

## Conventions
- Composition API only — no Options API
- `<script setup lang="ts">` in all new components
- `@` alias maps to `src/`
- No CSS framework — styles are component-scoped `<style scoped>`
- Enums from models match the API's string enum values exactly (e.g. `"Draft"`, `"Sent"`)
