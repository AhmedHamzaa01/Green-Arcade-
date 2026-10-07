# Green Arcade MVP — Implementation Plan

Steps are ordered by dependency: each step only uses things built in earlier steps. Do one step per Claude Code session, committed directly to `main`; check its "Done when" list, then move on.

| Step | Name | Depends on | Covers |
| --- | --- | --- | --- |
| 0 | Confirm decisions | — | BRD §7 |
| 1 | Repo + backend skeleton | 0 | SRS §1, §2, §7; NFR-08 |
| 2 | Database + domain model | 1 | SRS §4 |
| 3 | Auth + roles | 2 | F1 · FR-01–04 · NFR-03 |
| 4 | Points ledger | 3 | F2 · FR-05–07 |
| 5 | Settings + audit log | 3 | FR-19 |
| 6 | Catalog (API) | 3, 5 | F5 · FR-13 |
| 7 | Cart, checkout, orders (API) | 4, 6 | F6, F7 · FR-14–18 |
| 8 | Submissions + moderation (API) | 4, 5 | F3 · FR-08–11 |
| 9 | Badges (API) | 7, 8 | F4 · FR-12 |
| 10 | Admin users + dashboard (API) | 4, 7, 8 | F8 |
| 11 | Angular skeleton + auth | 3 | F1 screens · NFR-02, NFR-10 |
| 12 | Store + account pages | 6, 7, 11 | F5–F7 screens |
| 13 | Actions pages | 8, 11 | F3 screens |
| 14 | Admin area | 10, 11 | F8 screens |
| 15 | Hardening + release | all | NFR-01–11 |

Order rationale: the points ledger (4) must exist before checkout (7) and approvals (8) because both write points. The backend for a feature comes before its pages, so each page is built against a real, tested API. Steps 11–14 can start in parallel with 6–10 once step 3 is done, if two people work on it.

---

## Step 0 — Confirm decisions (no code)

Answer and record in `docs/DECISIONS.md`. Defaults are listed so you can proceed if no answer arrives.

- [x] Photo-submission loop in MVP? → yes
- [x] Cash on delivery acceptable at launch? → yes, points + COD
- [x] Can one order mix points and cash? → no, one payment method per order
- [x] Purchase points go to the buyer? → yes
- [x] Points per submission category → 10 each, admin-editable
- [x] Purchase points → fixed reward points per product, awarded on delivery for points and COD orders (replaces the per-EGP rate)
- [x] UI library: Angular Material or PrimeNG → Angular Material

## Step 1 — Repo + backend skeleton

- Solution with Api / Application / Domain / Infrastructure / Tests projects and correct references.
- `docker-compose.yml` with PostgreSQL; connection string from user-secrets.
- Serilog, Problem Details, OpenAPI, `/health`, CORS for the Angular dev origin.

**Done when:** `dotnet build` passes; API runs; `/health` returns healthy; Swagger opens.

## Step 2 — Database + domain model

- Domain entities for every table in SRS §4 (except Identity tables) with enums for statuses and types.
- `AppDbContext`, entity configurations, check constraints (`points_balance >= 0`, `stock >= 0`), indexes, snake_case.
- Identity tables (`asp_net_*`, Guid keys) so `user_id` columns have FKs from the start.
- Initial migration; seed submission categories, product categories, roles, `daily_submission_limit`.

**Done when:** migration applies to a clean DB; tables visible in pgAdmin/DBeaver match SRS §4.

## Step 2b — Soft delete

- `deleted_at` + global query filter on catalog, category, badge and address tables; history tables refuse deletes; `asp_net_users.is_active`.

**Done when:** deleted rows are hidden but kept; a deleted slug can be reused; deleting a ledger row fails.

## Step 3 — Auth + roles

- Identity services on the Step 2 tables; register, login, refresh (rotation), verify email, forgot/reset password.
- `IEmailSender` with a dev implementation that logs the email.
- JWT config, role policies, seeded admin user from config.
- Rate limiting on `/auth/*`.

**Done when:** tests cover register → verify → login → refresh → reset; unverified user is blocked from member-only write endpoints.

## Step 4 — Points ledger (most critical step)

- `IPointsService` with `Earn`, `Redeem`, `Reverse`, `Adjust`; each inserts a ledger row and updates the balance in the caller's transaction with a row lock.
- `GET /me/points`.

**Done when:** tests prove: balance = ledger sum; negative balance rejected; two concurrent redeems cannot overspend (Testcontainers).

## Step 5 — Settings + audit log

- `settings` read/write service with typed keys; `GET/PUT /admin/settings`.
- Audit logging for all admin writes (filter or pipeline behaviour).

**Done when:** changing a setting is audit-logged and readable.

## Step 6 — Catalog API

- Admin CRUD for categories, products, variants, images (`IFileStorage`, local disk in dev).
- Public `GET /products` with paging, filter, search, sort; `GET /products/{slug}`.

**Done when:** inactive products hidden from public; image upload works; tests for filters.

## Step 7 — Cart, checkout, orders API

- Cart endpoints; `POST /orders` (points or COD) in one transaction with stock decrement and price snapshot.
- Order status transitions with history; cancel (stock back + Reverse redeemed points); Delivered writes an Earn row for the products' reward points (points and COD orders).

**Done when:** tests cover: insufficient stock, insufficient points, cancel refund, Delivered → reward points for both payment methods (awarded once), invalid status transition rejected.

## Step 8 — Submissions + moderation API

- `POST /submissions` (multipart, signature check, 10 MB, daily limit, weight_kg only for tracks_weight categories).
- Moderator queue, approve (Earn points, once only), reject (reason required).

**Done when:** tests cover daily limit, double-approve rejected, reject writes no points.

## Step 9 — Badges API

- Admin badge CRUD; evaluator runs after approval and after delivered order; awards once.

**Done when:** tests award each rule type exactly once.

## Step 10 — Admin users + dashboard API

- User search, role change, block, points adjustment (reason required); dashboard counts.

**Done when:** blocked user cannot log in; adjustment visible in ledger with admin id.

## Step 11 — Angular skeleton + auth

- Angular app, routing, layout (mobile-first), i18n-ready text, HTTP interceptor (JWT + refresh), auth/role guards, error toast from Problem Details.
- Login, sign-up, verify-email, reset-password pages.

**Done when:** a user can sign up, verify (dev email from logs), log in, and stay logged in across refresh.

## Step 12 — Store + account pages

- Home, product list/detail, cart, checkout (address → payment → review → confirm), order confirmation.
- Profile, edit profile + addresses, points history, my orders, order detail.

**Done when:** full flow works at 360 px width: browse → cart → checkout with points and with COD.

## Step 13 — Actions pages

- New submission (camera/file input, category, weight when needed, note), my submissions with status/reason.

**Done when:** submission from a phone browser reaches the admin queue.

## Step 14 — Admin area

- Lazy-loaded `/admin` with role guard: dashboard, submissions queue, products/categories, orders, users, settings, badges.

**Done when:** staff can run every F8 task without touching the database.

## Step 15 — Hardening + release

- Check every NFR; Lighthouse on store pages; backup + restore test; production storage and SMTP; deploy.
- Walk the PRD goals: sign-up → approved submission → order.

**Done when:** every FR and NFR ID is ticked off in a short checklist in `docs/DECISIONS.md`.
