# Green Arcade — Business Requirements Document (MVP)

Version 0.1 (draft) · 2026-10-01

## 1. Purpose and document control

The MVP of Green Arcade is a responsive web app (Angular + .NET + PostgreSQL) that combines a points engine, a photo-verified action loop, and Row-Cycle's merch store in one first release. This BRD states *why* the product is being built and *what the business needs*; the PRD defines product behaviour and the SRS defines the technical requirements.

| Item | Value |
| --- | --- |
| Product | Green Arcade (web MVP) |
| Company | Row-Cycle Inc., Alexandria, Egypt |
| Founder | Abdelmouez Shahin |
| Version | 0.1 (draft) |
| Related docs | PRD.md, SRS.md |
| Source | Green Arcade Build Roadmap (pitch deck + handwritten notes) |

## 2. Business background

Row-Cycle already runs real-world collection and clean-up activity; it has no digital layer that rewards participants or sells its products online. Green Arcade fills that gap.

- **What Row-Cycle does:** turns discarded aluminium cans (originally Red Bull cans) into recycled-aluminium products for sports and events, including medals and trophies.
- **Existing operations:** community can-collection drives with cafés, rowing clubs and volunteers; coastal and lake clean-ups across Egypt and Africa; partnerships with NGOs, universities and sports federations.
- **Rotahope:** a wave-based initiative that converts collected recycled material into funded chemotherapy sessions for children with cancer. Wave 1 converted 150 kg into 4 sessions (150 ÷ 4 = 37.5 kg per session). Wave 2 is live.

**Problem to solve:** participation is not recorded or rewarded in one place, field activity does not feed operational reporting, and the product line (medals, trophies, merch) has no online sales channel.

## 3. Business objectives

Target values are left blank on purpose: no baseline data exists yet, so Row-Cycle must set them.

| # | Objective | Success measure | Target |
| --- | --- | --- | --- |
| BO-1 | Open an online sales channel for Row-Cycle products | Orders placed per month; store revenue (EGP) | To be set by Row-Cycle |
| BO-2 | Reward and retain community participants | Monthly active users; approved submissions per month | To be set by Row-Cycle |
| BO-3 | Turn field activity into reportable data | Share of collection drives / clean-ups logged in the app; kg logged | To be set by Row-Cycle |
| BO-4 | Run one points economy across actions and shopping | Points earned vs. points redeemed per month | To be set by Row-Cycle |

## 4. MVP scope

Release 1 (MVP) = roadmap Phase 0 (foundation) + the Phase 1 earning loop + the Phase 5 consumer merch store. Web app only; Phase 6 is removed.

| Feature | Roadmap source | Release |
| --- | --- | --- |
| Auth, user profiles, roles | Phase 0 | **MVP** |
| Central points ledger | Phase 0 | **MVP** |
| Admin panel (moderation, catalog, orders) | Phase 0 | **MVP** |
| Photo-based action submission + manual approval | Phase 1 #1 | **MVP** |
| Badges (milestone-based) | Phase 1 #3 | **MVP** |
| Consumer merch store (catalog, cart, checkout, points redemption) | Phase 5 #10 | **MVP** |
| Daily mini-challenges for children | Phase 1 #2 | Release 2 |
| B2B bulk / custom-order request form (medals, trophies) | Phase 5 #10 | Release 2 |
| Online card payments (gateway) | Phase 5 | Release 2 (blocked on gateway licensing) |
| Rotahope wave tracker | Phase 5 #11b | Release 2 |
| Recycling / drop-off map | Phase 2 | Release 3 |
| Monthly leaderboard, competitions page | Phase 3 | Release 3 |
| Cash donations | Phase 5 #11a | Release 3 |
| Running tracker, athlete posts, rowing-board station | Phase 4 | Later |
| Native mobile apps | — | Later |
| Yearly seminar, arcade mini-games | Phase 6 | **Removed** |

## 5. Stakeholders

| Stakeholder | Interest in the MVP |
| --- | --- |
| Row-Cycle founder (Abdelmouez Shahin) | Business owner; approves scope and priorities |
| Youssef | Named in the roadmap as the person to clarify the store points mechanic |
| Row-Cycle staff / admins | Approve submissions, manage catalog, fulfil orders |
| Community members (volunteers, café and rowing-club participants) | Earn points, collect badges, shop |
| Customers | Buy medals, trophies and merch |
| Partners (NGOs, universities, sports federations) | Later releases: competitions, bulk orders |
| Development team | Builds and runs the platform |

## 6. High-level business requirements

| ID | Requirement |
| --- | --- |
| BR-01 | Users can register and keep a profile showing points balance and badges. |
| BR-02 | All points (earned from actions, earned from purchases, spent in the store) live in **one** ledger. There are never separate "game" and "store" points. |
| BR-03 | Users can submit a photo of a sustainable action; staff approve or reject it before points are awarded. |
| BR-04 | Submissions record Row-Cycle's real activity types (can collection, coastal/lake clean-up) so the data supports operational reporting. |
| BR-05 | The store sells Row-Cycle's real product line (medals, trophies, recycled-aluminium goods) plus branded merch. |
| BR-06 | Users can pay for store items with points, and each product awards a fixed number of reward points to the buyer. |
| BR-07 | Staff can manage the catalog, stock, orders and submissions from an admin panel. |
| BR-08 | The data model supports later B2B bulk orders and the Rotahope tracker without being rebuilt. |
| BR-09 | The web app works on mobile browsers, since most users will reach it by phone. |

## 7. Assumptions, constraints, risks, open questions

**Assumptions**

- Submission review is manual (staff) at launch, as the roadmap recommends.
- Without a licensed payment gateway, MVP checkout supports **points** and **cash on delivery** (confirmed 2026-10-04).
- Points from a purchase go to the **buyer**. Each product carries its own reward ("buying this earns X points"), awarded when the order is delivered, for every payment method (confirmed 2026-10-04).

**Constraints**

- Stack is fixed: Angular (web), .NET Web API, PostgreSQL.
- Web only for the MVP; no native mobile apps.
- Operations are Egypt-based; prices are in EGP.

**Risks**

| Risk | Effect | Mitigation |
| --- | --- | --- |
| Payment gateway not licensed in time | No online card payments | Ship points + cash on delivery first |
| Points abused (fake photos, repeat submissions) | Points lose value; store stock given away | Manual approval, daily limits, admin can reverse points |
| Store points priced too low or too high | Stock drains, or nobody redeems | Admin-configurable point prices |

**Open questions**

- [x] Store points mechanic → points go to the buyer; each product has its own reward points (2026-10-04)
- [x] Points and cash mixed in one order? → no, one payment method per order (2026-10-04)
- [x] Cash on delivery acceptable for launch → yes (2026-10-04)
- [ ] Which gateway (Paymob, Fawry, other) will follow?
- [x] Points per submission category → 10 each by default, admin-editable (2026-10-04)
- [ ] Who fulfils and ships orders, and to which cities?
