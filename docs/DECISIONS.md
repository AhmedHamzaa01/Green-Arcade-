# Decisions log

Append one line per decision. Newest at the bottom. If a decision changes a requirement, also edit BRD/PRD/SRS in the same commit.

| Date | Decision | Affects | Decided by |
| --- | --- | --- | --- |
| 2026-10-01 | MVP is a web app only (Angular + .NET + PostgreSQL) | BRD §4, SRS §1 | Abdelrahman |
| 2026-10-01 | Merch store (Phase 5, consumer flow) moved into the MVP; B2B bulk orders later | BRD §4 | Abdelrahman |
| 2026-10-01 | Phase 6 (seminar, arcade games) removed | BRD §4 | Abdelrahman |
| 2026-10-04 | Photo-submission loop is in the MVP | BRD §4 | Ahmed Hamza |
| 2026-10-04 | Checkout supports points and cash on delivery at launch; card gateway still open | BRD §7, PRD F6 | Ahmed Hamza |
| 2026-10-04 | One payment method per order (no points + cash mix) | PRD F6, FR-14 | Ahmed Hamza |
| 2026-10-04 | Purchase points go to the buyer | BRD §7, BR-06 | Ahmed Hamza |
| 2026-10-04 | Each submission category awards 10 points by default (admin-editable) | PRD F3, `submission_categories.points_reward` | Ahmed Hamza |
| 2026-10-04 | UI library: Angular Material | SRS §1 | Ahmed Hamza |
| 2026-10-04 | Purchase points are a fixed `reward_points` per product (shown as "earn X points"), awarded to the buyer at checkout for points and COD orders; replaces the per-EGP rate. Cancelling reverses them | BRD §6–7, PRD F5–F8, FR-14/15/17/18, SRS §4 | Ahmed Hamza |
| 2026-10-04 | Product reward points are awarded when the order is Delivered, not at checkout (supersedes the previous row). Cancellation is only possible before delivery, so rewards never need reversing | BRD §7, PRD F6–F7, FR-14/17/18, Step 7 | Ahmed Hamza |
| 2026-10-06 | Code, solution and projects are named RowCycle (`RowCycle.*`); the product stays "Green Arcade" | SRS §7, CLAUDE.md | Ahmed Hamza |
| 2026-10-06 | Redis removed from docker-compose; nothing in the MVP needs it | SRS §7 | Ahmed Hamza |
| 2026-10-06 | API docs: built-in .NET OpenAPI document + Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`); Swashbuckle generator dropped | SRS §1 | Ahmed Hamza |
| 2026-10-07 | Identity tables (`asp_net_*`, Guid keys) and the four roles are created in Step 2's initial migration so `user_id` columns get real FKs from the start; Step 3 adds only the auth features | IMPLEMENTATION_PLAN Steps 2–3 | Ahmed Hamza |
| 2026-10-07 | Enum columns (statuses, types, payment method) are stored as text, e.g. `Placed`, `Earn` | SRS §4 | Ahmed Hamza |
| 2026-10-07 | `products.created_at` added for the "newest" sort; `audit_logs.entity_id` is text so it can hold a uuid or a settings key | SRS §4, PRD F5, FR-19 | Ahmed Hamza |
| 2026-10-07 | Soft delete (`deleted_at`, hidden by a global query filter) for products, variants, images, product/submission categories, badges, addresses. Ledger, orders, order items, status history, submissions and audit logs are never deleted (the code refuses). Cart items and user badges are hard-deleted. Users are deactivated (`is_active`), never deleted. Unique slugs/SKUs/names ignore deleted rows | SRS §4, FR-05, FR-16, FR-19 | Ahmed Hamza |
| 2026-10-07 | Work is committed directly to `main`; no per-step branches | IMPLEMENTATION_PLAN | Ahmed Hamza |
| 2026-10-07 | Refresh tokens are stored hashed (SHA-256) in a new `refresh_tokens` table; each is single-use, and reusing a spent token revokes all of the user's sessions. `POST /auth/logout` revokes one refresh token (PRD F1 "log out") | SRS §4–5, FR-03 | Ahmed Hamza |
| 2026-10-07 | Email verification links last 24 h; password reset links 1 h (FR-04). Login locks for 15 min after 5 wrong passwords. `/auth/*` is limited to 10 requests per minute per IP | FR-02–04, NFR-03 | Ahmed Hamza |
| 2026-10-07 | Unverified members can log in and browse; the `VerifiedMember` policy blocks them from submitting and checking out (FR-02) | FR-02, PRD F1 | Ahmed Hamza |
| 2026-10-07 | First admin is created on startup from `Admin:Email` / `Admin:Password` (user-secrets or env vars) | Step 3 | Ahmed Hamza |
| 2026-10-08 | Business logic lives in Application services (e.g. `AuthService`); controllers stay thin. Infrastructure only implements small interfaces Application defines (`IIdentityService`, `IRefreshTokenStore`, `IAccessTokenIssuer`, `IUnitOfWork`, repositories). Expected errors are `AppException`s mapped to Problem Details | SRS §2, CLAUDE.md rule 3 | Ahmed Hamza |
| 2026-10-09 | Points: `Reverse` undoes exactly what a source (order/submission) did — the opposite of its net ledger effect — and only once; nobody types the amount. A unique index allows one Earn, one Redeem and one Reverse per source. Not enough points returns 409 | FR-05, FR-06, FR-17 | Ahmed Hamza |
| 2026-10-09 | Points methods join the caller's transaction when one is open (checkout, approval), so the order/stock/status writes and the ledger row commit or roll back together | FR-10, FR-14, FR-17, FR-18, NFR-05 | Ahmed Hamza |
| 2026-10-09 | Ledger, order status history and audit rows are append-only in code too (updates and deletes throw) | FR-05, FR-16, FR-19 | Ahmed Hamza |
| 2026-10-09 | Shared building blocks: `IRepository<T>` (find/add/remove), `PageRequest` (page ≥ 1, pageSize 1–100, default 20) and `PagedResult<T>`; enums are sent as text in JSON | SRS §5 | Ahmed Hamza |
| 2026-10-09 | AutoMapper maps entities to DTOs (one `Profile` per feature, validated by a test). It is commercial since v15: development/testing run without a key (warning in the log); production needs a Lucky Penny licence, set as `AutoMapper:LicenseKey` | SRS §1, CLAUDE.md | Ahmed Hamza |
| 2026-10-09 | The balance row lock uses SQL (`SELECT ... FOR UPDATE`) because LINQ has no locking operator; all other queries use LINQ | FR-06 | Ahmed Hamza |
| 2026-10-10 | Audit logging is an explicit `IAuditLogService.Record(...)` call inside each admin service, saved in the same transaction as the change, with meaningful data (e.g. old/new value), instead of an automatic endpoint filter. Unchanged values write no row | FR-19, Step 5 | Ahmed Hamza |
| 2026-10-10 | `GET /admin/audit-logs` (Admin) lists the audit log, paged and filterable | SRS §5, FR-19 | Ahmed Hamza |
| 2026-10-10 | `daily_submission_limit` is editable between 1 and 50 (default 5) | FR-09 | Ahmed Hamza |
| 2026-10-10 | The product is renamed from "Green Arcade" to **RowCycle** everywhere users see it (UI, emails, Swagger) and in the docs; supersedes the 2026-10-06 row. The dev database keeps the name `greenarcade_db` | BRD, PRD, SRS, CLAUDE.md | Ahmed Hamza |
| 2026-10-10 | Refresh token moves from the JSON body to an httpOnly, SameSite=Strict cookie (`rc_refresh`, path `/api/v1/auth`, Secure outside Development). `/auth/refresh` and `/auth/logout` take no body. The web app keeps the access token in memory only. Deployment requirement: web app and API on the same site (e.g. `rowcycle.example` + `api.rowcycle.example`) | FR-03, NFR-03, SRS §5 | Ahmed Hamza |
| 2026-10-10 | Frontend: Angular 22 (needs Node 22.22+), Angular Material, Transloco for UI text (`assets/i18n/en.json`, Arabic later), hand-written TypeScript models and services per feature, dev proxy `/api` → `localhost:5130` | SRS §1, NFR-10 | Ahmed Hamza |
| 2026-10-10 | First UI round covers the APIs that exist: auth pages, profile, points history, and admin settings + audit log (admin screens pulled ahead of Step 14) | IMPLEMENTATION_PLAN Steps 11, 14 | Ahmed Hamza |
| 2026-10-10 | Catalog: a new product gets one "Default" variant (stock 0); the last variant can't be deleted; a category with products can't be deleted (409) — deactivate or move the products instead. Slugs are made from names ("-2", "-3"… if taken) unless given | F5, FR-13, SRS §5 | Ahmed Hamza |
| 2026-10-10 | Shoppers see whether a variant is in stock, not the exact stock count; staff see the counts. Stock changes are audit-logged with old and new values | F5, FR-19 | Ahmed Hamza |
| 2026-10-10 | Product photos: JPG/PNG/WEBP up to 10 MB, checked by file signature, saved with random names in `Storage:LocalPath` (dev: `<repo>/storage`, gitignored; Docker: `uploads` volume) and served read-only at `/media` with `nosniff`. Store managers see the staff menu (Products, Categories); Settings and Audit log stay Admin-only | NFR-04, FR-08, PRD roles | Ahmed Hamza |
