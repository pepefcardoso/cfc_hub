## EPIC 26 — Frontend: Scaffolding & Infrastructure

---

### TASK-083 — Bootstrap Next.js application

**Layer:** Frontend  
**Description:**  
Initialize the Next.js 14 App Router application with TypeScript, Tailwind CSS, and shadcn/ui. Establish the project structure, path aliases, environment variable schema, and Docker integration for local dev.

**Files to create:**

```

frontend/package.json
frontend/tsconfig.json
frontend/next.config.ts
frontend/tailwind.config.ts
frontend/components.json
frontend/.env.example
frontend/src/app/layout.tsx
frontend/src/app/globals.css
frontend/src/lib/env.ts
frontend/Dockerfile

```

**`frontend/.env.example`:**

```

NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api/v1
NEXT_PUBLIC_APP_ENV=dev

```

**`docker-compose.yml` addition:**

```yaml
web:
  build:
    context: ./frontend
    dockerfile: Dockerfile
  ports:
    - "3000:3000"
  depends_on:
    - api
  environment:
    - NEXT_PUBLIC_API_BASE_URL=http://api:8080/api/v1
    - NEXT_PUBLIC_APP_ENV=dev
```

**`frontend/src/lib/env.ts`:**  
Validates all required `NEXT_PUBLIC_*` variables at boot using `zod`. Throws at build time if any are missing. Never exposes server secrets to the client bundle.

**Path aliases in `tsconfig.json`:**

- `@/components` → `src/components`
- `@/lib` → `src/lib`
- `@/hooks` → `src/hooks`
- `@/types` → `src/types`

**Dockerfile:** multi-stage; `node:22-alpine` for build, same for runtime; runs as non-root `node` user; `NEXT_PUBLIC_*` vars are baked at build time via `ARG`.

**Acceptance Criteria:**

- `npm run dev` starts the dev server on port 3000
- `npm run build` succeeds with zero TypeScript errors
- `npm run lint` passes with zero errors
- `docker compose up web` starts without errors
- `frontend/src/lib/env.ts` throws a descriptive error at startup if `NEXT_PUBLIC_API_BASE_URL` is missing
- shadcn/ui components install with `npx shadcn@latest add <component>` without config changes

**Depends on:** TASK-002

---

### TASK-084 — Implement typed API client with auth and error handling

**Layer:** Frontend  
**Description:**  
Implement a typed API client layer that handles authentication headers, response envelope unwrapping, cursor pagination, and `ProblemDetails` error normalization. All domain API calls go through this layer — no raw `fetch` calls in components.

**Files to create:**

```
frontend/src/lib/api/client.ts
frontend/src/lib/api/errors.ts
frontend/src/lib/api/types.ts
frontend/src/lib/api/auth.ts
frontend/src/lib/api/scheduling.ts
frontend/src/lib/api/enrollment.ts
frontend/src/lib/api/contracts.ts
frontend/src/lib/api/finance.ts
frontend/src/lib/api/compliance.ts
frontend/src/lib/api/identity.ts
```

**`client.ts` implementation notes:**

- Base `apiFetch<T>(path, options)` wraps `fetch`, appends `Authorization: Bearer {token}` from session, and returns typed `T` extracted from the `data` field of the envelope
- On `4xx/5xx`: parse `ProblemDetails` JSON and throw typed `ApiError` with `status`, `type`, `detail`, `errors` (field-level map), `traceId`
- On network failure: throw `NetworkError`
- All functions are `async` and accept an optional `signal: AbortSignal`

**`types.ts`:**

```typescript
export interface PaginatedResponse<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
  count: number;
}

export interface ApiError {
  status: number;
  type: string;
  detail: string;
  errors?: Record<string, string[]>;
  traceId: string;
}
```

**`auth.ts`:** `login(email, password)`, `logout()`. Stores JWT in an `httpOnly`-equivalent mechanism via a Next.js Route Handler that proxies to the API (token never stored in `localStorage`). Exposes `getSession()` → `{ accessToken, role, tenantId, expiresAt } | null`.

**Pagination helper:**

```typescript
export async function fetchAllPages<T>(
  fetcher: (cursor: string | null) => Promise<PaginatedResponse<T>>,
): Promise<T[]>;
```

**Acceptance Criteria:**

- `apiFetch` automatically attaches the Bearer token on every call
- `401` response clears the session and redirects to `/login`
- `ApiError.errors` maps field names to pt-BR messages for inline form validation
- Network errors are distinguishable from API errors by type
- All domain modules (`scheduling.ts`, `enrollment.ts`, etc.) export typed functions — no raw `fetch` outside `client.ts`
- Unit test: `apiFetch_WithExpiredToken_RedirectsToLogin`

**Depends on:** TASK-083

---

### TASK-085 — Design system: tokens, layout primitives, and shared components

**Layer:** Frontend  
**Description:**  
Establish the visual design system: color tokens, typography scale, spacing, and a set of shared components used across all modules. All components must be accessible (WCAG AA), keyboard-navigable, and responsive.

**Files to create:**

```
frontend/src/components/ui/          (shadcn auto-generated — do not hand-edit)
frontend/src/components/shared/PageHeader.tsx
frontend/src/components/shared/DataTable.tsx
frontend/src/components/shared/CursorPagination.tsx
frontend/src/components/shared/EmptyState.tsx
frontend/src/components/shared/ErrorBoundary.tsx
frontend/src/components/shared/ConfirmDialog.tsx
frontend/src/components/shared/StatusBadge.tsx
frontend/src/components/shared/FieldRedacted.tsx
frontend/src/components/shared/FormSection.tsx
frontend/src/components/shared/LoadingSpinner.tsx
frontend/src/styles/tokens.css
```

**Design tokens (`tokens.css`):**  
Define CSS custom properties for the CFCHub brand palette, mapped to Tailwind via `tailwind.config.ts`. Supports `light` and `dark` modes via `[data-theme]` attribute.

**`DataTable<T>` props:** `columns`, `data`, `isLoading`, `emptyMessage`. Renders a `<table>` with accessible headers; `isLoading` shows skeleton rows; `emptyMessage` renders `<EmptyState>`.

**`CursorPagination` props:** `hasMore`, `nextCursor`, `onNext`, `isLoading`. Renders a single "Carregar mais" button; never shows page numbers (cursor-based only, per `conventions.md §5`).

**`StatusBadge` props:** `status` string; maps domain enum values (`Confirmed`, `Pending`, `Active`, etc.) to pt-BR labels and Tailwind color variants.

**`FieldRedacted`:** renders `—` with a lock icon and `title="Sem permissão para visualizar"` when a field value is `null` due to field-level access control. Used in student detail screens.

**`ConfirmDialog` props:** `title`, `description` (pt-BR), `onConfirm`, `onCancel`, `destructive?: boolean`. Wraps shadcn `AlertDialog`.

**Acceptance Criteria:**

- `DataTable` is keyboard-navigable and screen-reader compatible (proper `scope` on headers)
- `CursorPagination` never uses offset-based controls (no page numbers, no skip)
- `FieldRedacted` is rendered wherever the API returns `null` for a restricted field — verified by Storybook story
- `StatusBadge` covers all status enums from all domain modules
- `ErrorBoundary` catches render errors and shows a pt-BR fallback with a "Tentar novamente" button
- All components pass `npm run lint` and TypeScript strict mode

**Depends on:** TASK-083

---

## EPIC 27 — Frontend: Authentication & App Shell

---

### TASK-086 — Login page and session management

**Layer:** Frontend  
**Description:**  
Implement the login page, session lifecycle (including expiry refresh), and Next.js Route Handlers that proxy auth calls to the backend to avoid exposing the JWT in the browser.

**Files to create:**

```
frontend/src/app/(auth)/login/page.tsx
frontend/src/app/(auth)/login/LoginForm.tsx
frontend/src/app/api/auth/login/route.ts
frontend/src/app/api/auth/logout/route.ts
frontend/src/lib/auth/session.ts
frontend/src/middleware.ts
```

**`LoginForm.tsx`:**

- Fields: `email` (type=email), `senha` (type=password)
- Validation via `react-hook-form` + `zod`: email format; password min 8 chars
- On submit: calls `POST /api/auth/login` (local Route Handler)
- On success: redirects to `/agenda`
- On `401` or `403 (ACCOUNT_INACTIVE)`: shows pt-BR error inline, never clears the form password (UX: avoid re-typing email)
- Shows `LoadingSpinner` during submission; disables submit button
- Rate limit feedback: if `429` returned, shows "Muitas tentativas. Aguarde {Retry-After}s."

**`/api/auth/login` Route Handler:**

- Proxies to `POST /api/v1/auth/login` on the backend
- On success: sets `session` cookie (`httpOnly`, `sameSite=strict`, `secure` in prod) containing the JWT
- Never returns the raw JWT to the browser response body

**`session.ts`:** `getSession()`, `clearSession()`. Reads/writes the `httpOnly` session cookie via Next.js `cookies()`. Decodes JWT claims (without re-verifying — verification happens at the API) to expose `{ role, tenantId, userId, expiresAt }`.

**`middleware.ts`:** Runs on all routes except `/(auth)/**` and `/api/auth/**`. Reads the session cookie. If missing or expired → redirects to `/login?redirect={pathname}`. After login, restores the original destination.

**Acceptance Criteria:**

- JWT is never accessible via `document.cookie` or JavaScript (httpOnly)
- `401` from the backend shows "Credenciais inválidas." — no mention of which field was wrong (prevents user enumeration)
- `403 (ACCOUNT_INACTIVE)` shows "Conta inativa. Contate o administrador."
- Middleware redirects unauthenticated requests to `/login` and restores destination after login
- `npm run build` confirms no JWT value appears in the client bundle
- Test: `LoginForm_SubmitWithInvalidCredentials_ShowsErrorInline`

**Depends on:** TASK-084

---

### TASK-087 — App shell: sidebar, topbar, and tenant context

**Layer:** Frontend  
**Description:**  
Implement the authenticated layout shell: collapsible sidebar with role-filtered navigation, topbar with tenant name and user menu, and a React context that exposes the current session (role, tenantId) to all child components.

**Files to create:**

```
frontend/src/app/(dashboard)/layout.tsx
frontend/src/components/shell/Sidebar.tsx
frontend/src/components/shell/Topbar.tsx
frontend/src/components/shell/UserMenu.tsx
frontend/src/components/shell/NavItem.tsx
frontend/src/context/SessionContext.tsx
```

**Navigation items and role visibility:**

| Route                     | pt-BR Label  | Roles               |
| ------------------------- | ------------ | ------------------- |
| `/agenda`                 | Agenda       | All                 |
| `/alunos`                 | Alunos       | Admin, Receptionist |
| `/contratos`              | Contratos    | Admin, Receptionist |
| `/financeiro`             | Financeiro   | Admin, Financial    |
| `/conformidade`           | Conformidade | Admin, Receptionist |
| `/configuracoes/usuarios` | Usuários     | Admin               |

**`SessionContext`:** provides `{ role, tenantId, userId }` via `useSession()` hook. Populated from the decoded JWT in the session cookie (read server-side in layout, passed as a prop to the client context provider).

**Responsive behavior:** sidebar collapses to icons-only at `md` breakpoint; on `sm` it overlays as a drawer triggered by a hamburger button in the topbar.

**`UserMenu`:** shows user name and role badge; "Sair" option calls `POST /api/auth/logout` then redirects to `/login`.

**Acceptance Criteria:**

- Navigation items invisible to unauthorized roles are not rendered (not just hidden)
- `useSession()` throws if called outside `SessionContext.Provider`
- Sidebar state (open/collapsed) persists in `localStorage` between page loads
- Topbar displays tenant slug resolved from the JWT `tenant_id` claim
- Keyboard: sidebar toggle and user menu fully navigable without a mouse
- Test: `Sidebar_AsReceptionist_DoesNotShowFinanceLink`

**Depends on:** TASK-085, TASK-086

---

### TASK-088 — Role-based route guards and permission hooks

**Layer:** Frontend  
**Description:**  
Implement a composable permission layer — a `usePermission` hook and `<RequireRole>` guard component — that enforces role-based access on the client side. Server-side enforcement remains authoritative; this is a UX layer only.

**Files to create:**

```
frontend/src/hooks/usePermission.ts
frontend/src/components/auth/RequireRole.tsx
frontend/src/lib/permissions.ts
```

**`permissions.ts`:** mirrors the backend `FieldAccessPolicy` — a static map of `{ role → string[] allowedFields }` and `{ role → RouteType[] allowedRoutes }`. Updated in sync with `GEMINI.md §6.6` whenever the backend policy changes.

**`usePermission(field: string): boolean`:** reads current role from `useSession()`, checks against `permissions.ts`. Use for conditional rendering of individual fields.

**`<RequireRole roles={[...]}>`:** wraps children; renders `null` (not a redirect) if current role is not in the list. Used for sections within a page (e.g., hiding the "Novo usuário" button for non-admins).

**Full-page route protection** stays in `middleware.ts` (TASK-086) — `RequireRole` is only for sub-page sections.

**Acceptance Criteria:**

- `usePermission("Student.Cpf")` returns `false` for `Receptionist` role
- `<RequireRole roles={["Admin"]}>` renders `null` for all other roles — verified by test
- `permissions.ts` is the single source of truth for all client-side permission checks — no inline role comparisons in page components
- Test: `RequireRole_AsInstructor_DoesNotRenderAdminContent`

**Depends on:** TASK-087

---

## EPIC 28 — Frontend: Identity Module UI

---

### TASK-089 — Staff user management screens

**Layer:** Frontend  
**Description:**  
Implement the staff user list, invite (create), role change, and deactivation screens. Admin role only.

**Files to create:**

```
frontend/src/app/(dashboard)/configuracoes/usuarios/page.tsx
frontend/src/app/(dashboard)/configuracoes/usuarios/StaffUserTable.tsx
frontend/src/app/(dashboard)/configuracoes/usuarios/InviteUserDialog.tsx
frontend/src/app/(dashboard)/configuracoes/usuarios/ChangeRoleDialog.tsx
frontend/src/hooks/useStaffUsers.ts
```

**`StaffUserTable`:** columns: Nome, Papel (badge), Status (badge), Último acesso, Ações. "Ações" column shows role change and deactivate buttons for `Admin` only (via `<RequireRole>`). Cursor-paginated with `<CursorPagination>`. Renders `FieldRedacted` for `email` if the current user's role denies access (mirrors backend field policy).

**`InviteUserDialog`:** form fields: nome, e-mail, papel (select), senha temporária. Zod schema enforces the same password policy as the backend validator (min 12 chars, uppercase, number, special char). On success: shows a toast "Usuário convidado com sucesso." and refreshes the table. On `409 (EMAIL_IN_USE)`: shows inline error on the email field.

**`ChangeRoleDialog`:** select with available `RoleType` values labeled in pt-BR. Disabled for self (cannot demote yourself). On success: optimistic update on the row.

**Deactivate:** uses `<ConfirmDialog destructive>` before calling the API. On success: updates row status badge to "Inativo".

**`useStaffUsers` hook:** manages cursor state, loading, and `SWR`/`React Query` caching. Exposes `{ users, isLoading, hasMore, loadMore, mutate }`.

**Acceptance Criteria:**

- Page is not accessible for non-Admin roles (middleware blocks it)
- `InviteUserDialog` shows password strength feedback inline
- `ChangeRoleDialog` is disabled when the target user is the current user
- Deactivate action requires confirmation dialog before API call
- Table uses `<CursorPagination>` — no skip/offset controls
- `409` on duplicate email maps to the email field error inline

**Depends on:** TASK-087, TASK-088, TASK-084

---

## EPIC 29 — Frontend: Scheduling Module UI

---

### TASK-090 — Availability calendar and slot booking

**Layer:** Frontend  
**Description:**  
Implement the availability view (weekly calendar grid showing free 50-minute slots) and the booking flow. This is the most frequently used screen in the application.

**Files to create:**

```
frontend/src/app/(dashboard)/agenda/page.tsx
frontend/src/app/(dashboard)/agenda/AvailabilityCalendar.tsx
frontend/src/app/(dashboard)/agenda/SlotCard.tsx
frontend/src/app/(dashboard)/agenda/BookSlotDialog.tsx
frontend/src/app/(dashboard)/agenda/DateNavigator.tsx
frontend/src/hooks/useAvailability.ts
```

**`AvailabilityCalendar`:**

- Displays a 7-column grid (Mon–Sun) with time rows from 06:00 to 20:00 in 50-minute increments
- Fetches `GET /scheduling/slots/available?date={date}&category={category}` for the selected week
- Available slots rendered as clickable `<SlotCard>` components showing instructor name and track type
- Booked slots (fetched separately for context) rendered as non-clickable, distinct colored cards
- Filters: CNH category selector, instructor selector (optional). Both filter `useAvailability` query params
- Loading state: skeleton grid

**`BookSlotDialog`:** opens when the user clicks a `<SlotCard>`. Shows slot details (date/time, instructor, vehicle, track). Student selector (searchable combobox, calls `GET /alunos?q=...`). Confirm button calls `POST /scheduling/slots`. On `409` (lock or overlap conflict): shows "Horário indisponível. Tente outro." without closing the dialog. On success: closes dialog, refreshes calendar, shows toast "Aula agendada com sucesso."

**`useAvailability` hook:** fetches available slots for the current selected date and filters. Invalidates cache after any booking action.

**Acceptance Criteria:**

- Calendar shows current week by default; `DateNavigator` allows ±4 week navigation
- Clicking a booked slot does nothing (no dialog opens)
- `409` on booking keeps the dialog open with an inline error — no page reload
- CNH category filter changes the `category` query param and refetches
- Loading state shown while fetching (skeleton, not spinner, to avoid layout shift)
- Test: `BookSlotDialog_OnConflict_ShowsInlineErrorWithoutClosing`

**Depends on:** TASK-085, TASK-087, TASK-084

---

### TASK-091 — Slot management: views, cancel, complete, no-show

**Layer:** Frontend  
**Description:**  
Implement the instructor's daily slot list, student's upcoming lessons view, and state transition actions (cancel, complete, no-show).

**Files to create:**

```
frontend/src/app/(dashboard)/agenda/instrutor/[instructorId]/page.tsx
frontend/src/app/(dashboard)/agenda/aluno/[studentId]/page.tsx
frontend/src/components/scheduling/SlotListItem.tsx
frontend/src/components/scheduling/SlotActions.tsx
frontend/src/hooks/useInstructorSlots.ts
frontend/src/hooks/useStudentSlots.ts
```

**`SlotListItem`:** shows time range, instructor name, vehicle, track type, CNH category, and `<StatusBadge>`. Renders `<SlotActions>` inline.

**`SlotActions`:** renders action buttons based on slot status and current user role:

- `Confirmed` + `Admin|Receptionist` → "Cancelar" (opens `<ConfirmDialog>` with reason textarea)
- `Confirmed` + `Instructor|Admin` → "Concluir", "Falta"
- `Cancelled|Completed|NoShow` → no actions

All actions call their respective `PATCH` endpoints. On success: optimistic status update on the `SlotListItem`. On error: revert and show toast.

**Student slot view (`/agenda/aluno/[studentId]`):** cursor-paginated list of all slots for a student, ordered by `started_at` descending. Student can only cancel their own future confirmed slots.

**Acceptance Criteria:**

- Student cannot see the "Concluir" or "Falta" buttons (role guard)
- "Cancelar" requires a reason (min 10 chars in the `ConfirmDialog` textarea)
- Optimistic update reverts correctly on API error
- Cursor pagination loads additional slots without losing scroll position
- Test: `SlotActions_AsStudent_DoesNotRenderCompleteButton`

**Depends on:** TASK-090

---

## EPIC 30 — Frontend: Enrollment Module UI

---

### TASK-092 — Student registration with LGPD consent form

**Layer:** Frontend  
**Description:**  
Implement the multi-step student registration form. Step 3 (consent) is mandatory and blocks submission if not completed — enforcing the LGPD consent mandate from `GEMINI.md §6.5`.

**Files to create:**

```
frontend/src/app/(dashboard)/alunos/novo/page.tsx
frontend/src/app/(dashboard)/alunos/novo/StepPersonalData.tsx
frontend/src/app/(dashboard)/alunos/novo/StepAddress.tsx
frontend/src/app/(dashboard)/alunos/novo/StepConsent.tsx
frontend/src/app/(dashboard)/alunos/novo/RegistrationStepper.tsx
frontend/src/lib/cpf.ts
```

**Multi-step flow:**

1. **Dados pessoais:** nome, CPF (masked input + Luhn validation via `cpf.ts`), RG (optional), e-mail, telefone (+55 format), data de nascimento (must be 16–100 years ago)
2. **Endereço:** rua, número, complemento (optional), bairro, cidade, estado (dropdown with `BrazilianState` enum), CEP (8-digit mask + optional ViaCEP autofill)
3. **Consentimento LGPD:** shows the current policy text (fetched from a static asset or config); checkbox "Li e aceito a Política de Privacidade versão {version}"; the submit button is disabled until checkbox is checked; records `policyVersion` and `SHA-256(policyText)` sent to the API

**`cpf.ts`:** Brazilian CPF format mask and Luhn-algorithm validation (same rules as the backend `CreateStudentCommandValidator`).

On `409 (STUDENT_ALREADY_EXISTS)`: navigate back to step 1, show inline error on the CPF field "CPF já cadastrado no sistema."

On success: redirect to `/alunos/{studentId}` and show toast "Aluno cadastrado com sucesso."

**Acceptance Criteria:**

- Step 3 submit button is disabled until the consent checkbox is checked
- CPF field validates the Luhn algorithm client-side before submitting
- `policyVersion` and `policyContentHash` (SHA-256 of displayed text) are sent in the API request
- `409` on duplicate CPF returns to step 1 with inline error on the CPF field
- All validation messages are in pt-BR
- Test: `StepConsent_WithoutCheckbox_DisablesSubmitButton`

**Depends on:** TASK-085, TASK-087, TASK-084

---

### TASK-093 — Student list, detail, and enrollment screens

**Layer:** Frontend  
**Description:**  
Implement the student list with search, the student detail page with field-level access enforcement, and the enrollment creation flow.

**Files to create:**

```
frontend/src/app/(dashboard)/alunos/page.tsx
frontend/src/app/(dashboard)/alunos/[studentId]/page.tsx
frontend/src/app/(dashboard)/alunos/[studentId]/EnrollmentCard.tsx
frontend/src/app/(dashboard)/alunos/[studentId]/NewEnrollmentDialog.tsx
frontend/src/components/enrollment/StudentDetailCard.tsx
frontend/src/hooks/useStudent.ts
frontend/src/hooks/useEnrollments.ts
```

**Student list (`/alunos`):** search input (debounced 300ms, updates `?q=` query param); cursor-paginated `<DataTable>` with columns: nome, CPF (renders `<FieldRedacted>` if null), status badge, ações (link to detail). "Novo aluno" button navigates to `/alunos/novo`.

**`StudentDetailCard`:** renders all student fields. Fields where the API returned `null` (e.g., `cpf`, `rg`, `phone` for Receptionist role) render `<FieldRedacted>`. No tooltip needed — the lock icon is self-explanatory.

**`EnrollmentCard`:** one card per enrollment showing CNH category, status badge, theory/practical hours progress bars (hours completed / threshold). "Nova Matrícula" button triggers `<NewEnrollmentDialog>`.

**`NewEnrollmentDialog`:** CNH category select. Calls `POST /students/{id}/enrollments`. On `409 (ENROLLMENT_ALREADY_EXISTS)`: shows "Aluno já matriculado nesta categoria." On success: optimistic append to enrollment list, toast "Matrícula realizada com sucesso."

**Acceptance Criteria:**

- CPF column renders `<FieldRedacted>` for roles without access — verified by test
- Search is debounced and updates the URL `?q=` param (shareable URL)
- Progress bars show correct percentage based on hours completed vs. category threshold
- `<NewEnrollmentDialog>` shows `409` inline without closing
- `DELETE /alunos/{id}` (erasure request) is only rendered for `Admin` role, behind a `<RequireRole>` guard

**Depends on:** TASK-092, TASK-088

---

## EPIC 31 — Frontend: Contracts Module UI

---

### TASK-094 — Contract viewer and digital signature

**Layer:** Frontend  
**Description:**  
Implement the contract list per student, the PDF viewer via pre-signed URL, and the digital signature capture flow.

**Files to create:**

```
frontend/src/app/(dashboard)/contratos/[contractId]/page.tsx
frontend/src/app/(dashboard)/contratos/[contractId]/ContractViewer.tsx
frontend/src/app/(dashboard)/contratos/[contractId]/SignatureCapture.tsx
frontend/src/hooks/useContract.ts
```

**`ContractViewer`:** fetches `GET /contracts/{contractId}` which returns a pre-signed S3 URL (TTL 3600s). Renders the PDF in an `<iframe>` or `<embed>` pointing to the pre-signed URL. Shows status badge and `SignedAt` date if already signed. Never exposes the S3 key.

**`SignatureCapture`:** visible only when `contract.status === 'Generated'`. Renders a `<canvas>` element for signature drawing using `signature_pad` library. "Limpar" button resets canvas. "Assinar Contrato" button:

1. Exports canvas as base64 PNG
2. Computes `SHA-256` of the image bytes in the browser
3. Calls `PATCH /contracts/{contractId}/sign` with `{ signatureHash, ipAddress: "resolved-client-side" }`
4. On `422 (CONTRACT_PENDING)`: shows "Contrato ainda sendo gerado. Aguarde e tente novamente."
5. On success: re-fetches contract, updates status badge, hides `SignatureCapture`

**Acceptance Criteria:**

- PDF is rendered via pre-signed URL — the raw S3 key never appears in client-side code
- Signature canvas blocks submission if empty (no strokes drawn)
- `422` on pending contract shows inline error without page reload
- After signing, the status badge updates without a full page reload
- Test: `SignatureCapture_WhenEmpty_DisablesSubmitButton`

**Depends on:** TASK-087, TASK-084, TASK-085

---

## EPIC 32 — Frontend: Finance Module UI

---

### TASK-095 — Payment plan and payment recording screens

**Layer:** Frontend  
**Description:**  
Implement the payment plan view per student enrollment, installment status tracking, and the payment recording form for financial staff.

**Files to create:**

```
frontend/src/app/(dashboard)/financeiro/alunos/[studentId]/page.tsx
frontend/src/app/(dashboard)/financeiro/alunos/[studentId]/PaymentPlanTable.tsx
frontend/src/app/(dashboard)/financeiro/alunos/[studentId]/RecordPaymentDialog.tsx
frontend/src/app/(dashboard)/financeiro/inadimplentes/page.tsx
frontend/src/hooks/usePaymentPlan.ts
frontend/src/hooks/useOverdueInstallments.ts
```

**`PaymentPlanTable`:** columns: parcela #, valor (BRL), vencimento, status badge, ações. "Registrar pagamento" action only shown for `Pending` or `Overdue` installments, only for `Admin|Financial` roles. Total amount and amount paid shown in a summary row at the bottom.

**`RecordPaymentDialog`:** fields: método de pagamento (select: Pix, Cartão, Boleto, Dinheiro). Calls `POST /payments` with `installmentId`, `method`, `amount` (pre-filled from installment, read-only). On success: optimistic update of installment status to "Pago", toast "Pagamento registrado."

**Overdue installments page (`/financeiro/inadimplentes`):** `Admin|Financial` only (middleware enforced). Table of all overdue installments across students. Cursor-paginated. Columns: aluno, parcela #, valor, vencimento, dias em atraso (computed client-side from `dueDate`).

**BRL formatting:** all monetary values formatted with `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`. Never display raw decimal strings.

**Acceptance Criteria:**

- `/financeiro/**` routes blocked for `Receptionist` and `Instructor` roles
- "Registrar pagamento" button not rendered for non-financial roles
- BRL amounts formatted with `R$` prefix and comma decimal separator
- Overdue installments page cursor-paginates correctly
- Test: `PaymentPlanTable_AsInstructor_DoesNotRenderPaymentAction`

**Depends on:** TASK-087, TASK-088, TASK-084, TASK-085

---

## EPIC 33 — Frontend: Compliance Module UI

---

### TASK-096 — Document tracking and expiry dashboard

**Layer:** Frontend  
**Description:**  
Implement the document expiry dashboard showing documents expiring within 30 days, color-coded by `AlertTier`, and the document registration form for uploading medical exams directly to S3.

**Files to create:**

```
frontend/src/app/(dashboard)/conformidade/page.tsx
frontend/src/app/(dashboard)/conformidade/ExpiryDashboard.tsx
frontend/src/app/(dashboard)/conformidade/ExpiryAlert.tsx
frontend/src/app/(dashboard)/conformidade/RegisterDocumentDialog.tsx
frontend/src/hooks/useExpiringDocuments.ts
```

**`ExpiryDashboard`:** calls `GET /compliance/documents/expiring`. Groups results by `AlertTier`: D1 (vermelho), D7 (laranja), D15 (amarelo), D30 (azul). Each group is an expandable section showing student name, document type (pt-BR label), expiry date, and days remaining.

**`RegisterDocumentDialog`:** for `MedicalExam` type:

1. File picker (accepts `application/pdf`, `image/jpeg`, `image/png`)
2. Calls `POST /compliance/documents` to get a pre-signed upload URL
3. Uploads directly from the browser to S3 using the pre-signed URL (no file passes through the Next.js server)
4. After S3 upload succeeds, calls `PATCH /compliance/documents/{id}/confirm` to record the `s3Key`
5. Shows upload progress via `XMLHttpRequest` progress events (not `fetch`, which doesn't expose upload progress)

**Acceptance Criteria:**

- D1 alerts highlighted in red with a warning icon
- File upload goes directly to S3 — no file bytes pass through the Next.js server
- Upload progress bar visible during S3 upload
- Invalid file type rejected client-side before any API call (accept attribute + type check)
- Documents older than the 30-day window do not appear
- Test: `ExpiryDashboard_GroupsAlertsByTier`

**Depends on:** TASK-087, TASK-084, TASK-085

---

### TASK-097 — DETRAN CNH status lookup

**Layer:** Frontend  
**Description:**  
Implement the CNH status lookup screen, accessible from a student's detail page. Handles graceful degradation when DETRAN is unavailable.

**Files to create:**

```
frontend/src/app/(dashboard)/alunos/[studentId]/cnh-status/page.tsx
frontend/src/components/compliance/CnhStatusCard.tsx
frontend/src/hooks/useCnhStatus.ts
```

**`CnhStatusCard`:** calls `GET /students/{studentId}/cnh-status`. Displays:

- `isAvailable: false` → "Consultar manualmente — sistema DETRAN indisponível no momento." (gray, no error state)
- `isAvailable: true` → status label, expiry date formatted as `dd/MM/yyyy`, pontuação
- Always shows last-fetched timestamp (from the response or cache age)

**Rate limit feedback:** if the API returns `429`, shows "Limite de consultas atingido. Aguarde {Retry-After}s." and disables the "Consultar novamente" button for that duration (countdown timer).

**`useCnhStatus` hook:** caches the result for 24h in memory (mirrors the 86400s Redis TTL). "Consultar novamente" button force-refetches (bypasses cache).

**Acceptance Criteria:**

- DETRAN unavailability renders a neutral gray card — not an error state
- `429` shows countdown timer and disables the refresh button
- Result is cached in memory for 24h — repeated visits don't re-fetch from the API
- CPF is never displayed or logged in the browser (only studentId is used in the URL)
- Test: `CnhStatusCard_WhenUnavailable_ShowsManualCheckMessage`

**Depends on:** TASK-093, TASK-084

---

## EPIC 34 — Frontend: Dashboard & Public Pages

---

### TASK-098 — Main dashboard with operational KPIs

**Layer:** Frontend  
**Description:**  
Implement the `/agenda` landing page (post-login default) showing the daily schedule summary and key operational metrics. This is the first screen users see after logging in.

**Files to create:**

```
frontend/src/app/(dashboard)/agenda/page.tsx
frontend/src/components/dashboard/DailySlotSummary.tsx
frontend/src/components/dashboard/KpiCard.tsx
frontend/src/components/dashboard/UpcomingSlotsList.tsx
frontend/src/components/dashboard/ExpiryAlertBanner.tsx
```

**Layout:** Two-column grid on `lg+`, single column on `sm`.

**`KpiCard`:** generic card with a title, numeric value, and trend indicator (up/down/neutral). Rendered for: aulas hoje, aulas esta semana, alunos ativos, parcelas vencidas (Financial role only).

**`UpcomingSlotsList`:** next 5 confirmed slots for today (fetched from `GET /scheduling/slots/available?date=today` combined with the instructor's booked slots). Clicking a slot navigates to `/agenda/instrutor/{instructorId}`.

**`ExpiryAlertBanner`:** sticky top banner (dismissible per session) shown only when `GET /compliance/documents/expiring` returns any D1 or D7 alerts. Links to `/conformidade`.

**Role-based KPI visibility:**

- `Receptionist`: aulas hoje, alunos ativos only
- `Instructor`: aulas hoje only (their own)
- `Financial`: parcelas vencidas only
- `Admin`: all KPIs

**Acceptance Criteria:**

- `/agenda` is the redirect destination after login
- `ExpiryAlertBanner` is only shown when D1/D7 alerts exist and not dismissed this session
- `KpiCard` for "parcelas vencidas" not rendered for non-Financial roles
- All data fetched in parallel (not sequentially)
- Page is server-rendered for the initial load (Next.js RSC where possible)

**Depends on:** TASK-087, TASK-088, TASK-084, TASK-085

---

### TASK-099 — Public CFC page and QR code landing

**Layer:** Frontend  
**Description:**  
Implement the two unauthenticated public routes: the CFC public profile page (marketing/info) and the QR code scan landing page (document verification). Both are accessible without authentication.

**Files to create:**

```
frontend/src/app/public/cfc/[slug]/page.tsx
frontend/src/app/public/qr/[code]/page.tsx
frontend/src/components/public/CfcPublicProfile.tsx
frontend/src/components/public/QrCodeResult.tsx
```

**`/public/cfc/[slug]`:** calls `GET /public/cfc/{slug}`. Renders CFC name, address, contact, and available CNH categories. No authentication required. Returns `notFound()` if the API returns 404.

**`/public/qr/[code]`:** calls `GET /public/qr/{code}`. Used for QR codes printed on generated documents to verify authenticity. Renders: document type, student name (anonymized if erased), issue date, validity status. Returns a clear "Documento não encontrado" message on 404.

**SEO:** both pages export `generateMetadata` with the CFC name or document type in the page title.

**Rate limit awareness:** `429` on the QR page shows "Muitas consultas. Aguarde e tente novamente." — no countdown needed (anonymous users).

**Acceptance Criteria:**

- Neither page requires authentication — middleware must exclude `/public/**`
- `notFound()` called on `404` from the API (renders Next.js 404 page)
- Both pages pass Lighthouse accessibility score ≥ 90
- `generateMetadata` returns a unique `title` and `description` per slug/code

**Depends on:** TASK-083, TASK-084, TASK-086

---

## EPIC 35 — Frontend: Testing & Quality

---

### TASK-100 — Component unit tests (Vitest + Testing Library)

**Layer:** Frontend  
**Description:**  
Implement unit tests for all shared components and critical domain components. Coverage target: 80%+ on `src/components/` and `src/hooks/`.

**Files to create:**

```
frontend/vitest.config.ts
frontend/src/tests/setup.ts
frontend/src/components/shared/DataTable.test.tsx
frontend/src/components/shared/CursorPagination.test.tsx
frontend/src/components/shared/FieldRedacted.test.tsx
frontend/src/components/auth/RequireRole.test.tsx
frontend/src/components/scheduling/SlotActions.test.tsx
frontend/src/components/enrollment/StudentDetailCard.test.tsx
frontend/src/lib/cpf.test.ts
frontend/src/lib/api/client.test.ts
```

**Required test cases:**

- `DataTable_WithLoadingTrue_ShowsSkeletonRows`
- `DataTable_WithEmptyData_ShowsEmptyState`
- `CursorPagination_WhenHasMoreFalse_DoesNotRenderButton`
- `FieldRedacted_WhenValueIsNull_RendersLockIcon`
- `FieldRedacted_WhenValueIsPresent_RendersValue`
- `RequireRole_AsReceptionist_DoesNotRenderAdminContent`
- `SlotActions_AsStudent_OnlyShowsCancelForOwnConfirmedSlot`
- `StudentDetailCard_AsReceptionist_CpfIsRedacted`
- `cpf_ValidateLuhn_CorrectlyValidatesKnownCpfs`
- `apiFetch_On401_ClearsSessionAndRedirects`
- `apiFetch_ParsesProblemDetailsErrors_IntoFieldMap`
- `LoginForm_SubmitWithInvalidEmail_ShowsValidationError`
- `StepConsent_WithoutCheckbox_DisablesSubmitButton`
- `BookSlotDialog_OnConflict409_ShowsInlineError`

**Tooling:** Vitest + `@testing-library/react` + `@testing-library/user-event`. MSW (`msw`) for API mocking — no inline `jest.fn()` mocks for HTTP calls.

**Acceptance Criteria:**

- `npm run test` runs all unit tests and exits 0
- Coverage report: `src/components/` ≥ 80%, `src/lib/` ≥ 85%
- No test uses `waitFor` with `Thread.Sleep`-equivalent (`setTimeout(fn, N)`) — use `@testing-library/user-event` async events
- All API calls in tests are intercepted by MSW handlers, never real HTTP
- CI pipeline (TASK-005) runs `npm run test -- --coverage` and fails below coverage floor

**Depends on:** TASK-088, TASK-090, TASK-092, TASK-094, TASK-095

---

### TASK-101 — End-to-end tests (Playwright)

**Layer:** Frontend  
**Description:**  
Implement critical-path end-to-end tests covering the flows with the highest business risk: login, slot booking, and student registration with consent.

**Files to create:**

```
frontend/playwright.config.ts
frontend/e2e/auth/login.spec.ts
frontend/e2e/scheduling/book-slot.spec.ts
frontend/e2e/enrollment/create-student.spec.ts
frontend/e2e/fixtures/auth.ts
frontend/e2e/fixtures/api.ts
```

**`auth.ts` fixture:** logs in as a given role before each test using the login API directly (bypasses UI for speed); stores session cookie.

**`api.ts` fixture:** seeds required test data (instructor, vehicle, track, available slot) by calling the backend API directly before the test; cleans up after. Uses a dedicated test tenant provisioned via `TenantProvisioningService`.

**Required E2E scenarios:**

- `Login_WithValidCredentials_RedirectsToAgenda`
- `Login_WithInvalidPassword_ShowsErrorInline`
- `Login_WithInactiveAccount_ShowsForbiddenMessage`
- `BookSlot_HappyPath_SlotAppearsInCalendar`
- `BookSlot_WhenConflict_ShowsInlineErrorAndKeepsDialogOpen`
- `CreateStudent_WithConsent_RedirectsToStudentDetail`
- `CreateStudent_WithoutConsentCheckbox_BlocksSubmission`
- `CreateStudent_WithDuplicateCpf_ShowsInlineErrorOnStep1`

**Playwright config:** runs against `http://localhost:3000` (requires `docker compose up`); `--reporter=html`; retries: 1 on CI; parallel workers: 2.

**Acceptance Criteria:**

- All 8 E2E tests pass against a live `docker compose up` environment
- Tests are fully isolated: each test creates and tears down its own data
- No hardcoded credentials in test files — sourced from `.env.test`
- CI pipeline (TASK-005) runs Playwright in a dedicated `e2e` job after the `integration-tests` job
- `playwright.config.ts` sets `baseURL` from `PLAYWRIGHT_BASE_URL` env var (defaults to `localhost:3000`)

**Depends on:** TASK-100, TASK-086, TASK-090, TASK-092

---

## Frontend Summary

| Epic                    | Tasks                | Focus                                    |
| ----------------------- | -------------------- | ---------------------------------------- |
| 26 — Scaffolding        | TASK-083 to TASK-085 | Next.js setup, API client, design system |
| 27 — Auth & Shell       | TASK-086 to TASK-088 | Login, layout, permissions               |
| 28 — Identity UI        | TASK-089             | Staff user management                    |
| 29 — Scheduling UI      | TASK-090 to TASK-091 | Calendar, booking, slot actions          |
| 30 — Enrollment UI      | TASK-092 to TASK-093 | Student registration (LGPD), list/detail |
| 31 — Contracts UI       | TASK-094             | PDF viewer, digital signature            |
| 32 — Finance UI         | TASK-095             | Payment plan, recording                  |
| 33 — Compliance UI      | TASK-096 to TASK-097 | Document expiry, DETRAN lookup           |
| 34 — Dashboard & Public | TASK-098 to TASK-099 | KPI dashboard, public pages              |
| 35 — Testing            | TASK-100 to TASK-101 | Vitest unit tests, Playwright E2E        |
