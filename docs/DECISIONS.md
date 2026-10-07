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
