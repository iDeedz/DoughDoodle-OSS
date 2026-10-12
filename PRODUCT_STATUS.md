# DoughDoodle product status (2026-09-10 clearance; last README sync 2026-09-25)

## Build / test
- `dotnet build DoughDoodle.sln` — succeeded
- `dotnet test` — 7/7 passed (SafeToSpend + Entitlements + RoadmapClearance)
- Migration: `AddRoadmapClearanceFeatures` (after `AddDoughBillingAndOnboarding`)
- `/health` endpoint shipped
- Dockerfile + `.github/workflows/ci.yml` shipped
- PWA: `wwwroot/manifest.webmanifest` + `service-worker.js` (v2) + `offline.html` + PNG/SVG icons

## Dough tiers (Stripe-ready; works with keys missing)
| Tier | Price label | Highlights |
|---|---|---|
| **Pocket** | Free forever | 10 envelopes, 2 accounts, CSV import, dough-left-today |
| **Bankroll** | $6/mo | Household share, pace alerts, advanced reports, 12-mo history |
| **Stack** | $10/mo | AI insights, deep CSV history, 24-mo history |
| **Vault** | $16/mo | Investments unlock, bigger household, 60-mo history |
| **Prime** | $24/mo | Unlimited envelopes/accounts/goals, 10-year history |

Configure `Stripe:*` in appsettings or env when ready. Billing page shows a setup message — no nagging.

## Branding paths
- `wwwroot/images/branding/logo-mark.svg`, `logo-mark.png`, `logo-wordmark.svg`, `hero-dough.svg`, `favicon.svg`, `favicon-32.png`, `empty-envelopes.svg`
- `wwwroot/images/tiers/tier-*.svg` + matching `tier-*.png`

## Roadmap Phases 1–20 (+ extras) shipped in-app
1. Advanced import — CSV/OFX/QFX/QIF + mapping templates
2. Bank sync — pluggable `IBankSyncProvider`; Manual/CSV; Plaid stub **only when keys configured**
3. Email — MailKit SMTP; bill/budget alerts when `Smtp:Host` set (SMS not shipped)
4. REST API — JWT + cookie; accounts/transactions/budgets ownership
5. PWA — verified + SW v2 offline improvements
6. Cashflow forecast page
7. Custom report builder
8. Net worth projections/charts
9. Financial calendar
10. Goal analytics
11. Household deepened (roles)
12. Business expense projects/reports
13. Collaborative comments
14. Team shared envelopes
15. `HouseholdRole` beyond IsAdmin
16. Offline-first SW improvements
17. Memory cache + API pagination
18. Theme polish
19. Command palette Ctrl+K
20. Undo/redo for transaction edits
- TOTP Authenticator 2FA (`/Settings/Security`)
- Multi-currency rates table (manual; API when `FxApi:ApiKey`)
- Mobile swipe on `[data-swipe-tx]` rows

## Honest backlog (still blocked on external partners / keys)
- **Live Plaid Link / Open Banking sync** — provider interface + honesty UI shipped; live token exchange needs Plaid ClientId/Secret + Link wiring
- **Live FX feeds** — rates table + manual convert shipped; auto refresh needs `FxApi:ApiKey`
- **SMS notifications** — needs Twilio (or similar) partner keys
- **CAMT.053** bank statement XML — not implemented (OFX/QFX/QIF/CSV are)
- **Push notifications** — PWA install/offline yes; Web Push not claimed
- **Biometric login / native mobile / blockchain / i18n** — Phase 21+ research items

## Sep 2026 sync notes (README)
- Onboarding starterSet and PreferredCurrency dough-left are in the app. Household polish on the paid share tier is still unfinished.
- Live Plaid Link still not claimed — BankSync only when keys set; CSV/manual remains the honest default.
