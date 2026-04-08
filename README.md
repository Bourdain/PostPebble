# PostPebble

Docker-first foundation for a multi-tenant social media management SaaS.

## Stack
- Backend: ASP.NET Core (.NET 10 preview)
- Frontend: React + TypeScript + Vite
- Infrastructure: Docker Compose (API, web, PostgreSQL, Redis)

## Quick start
1. Copy `.env.example` to `.env`.
2. Run:
   - `docker compose up --build`
3. Open:
   - Frontend: `http://localhost:5173`
   - API health: `http://localhost:8080/health`

## Implemented backend slices
- Tenant-aware authentication (JWT) and team membership endpoints
- Stripe-ready credit wallet ledger (purchase/reserve/consume/release)
- Dev credit grant endpoint for local testing
- Scheduler endpoints that reserve credits based on cross-post targets

## API highlights
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `GET /api/v1/tenants`
- `POST /api/v1/tenants/{tenantId}/members`
- `GET /api/v1/billing/wallets/{tenantId}`
- `GET /api/v1/billing/wallets/{tenantId}/transactions`
- `POST /api/v1/billing/credit-packs/checkout-session`
- `POST /api/v1/billing/stripe/webhook`
- `POST /api/v1/scheduler/posts`
- `POST /api/v1/scheduler/posts/{postId}/mark-success`
- `POST /api/v1/scheduler/posts/{postId}/mark-failed`

## Notes
- Schema is now migration-driven. If local DB drift from older manual tables causes issues, reset local volumes once:
  - `docker compose down -v`
  - `docker compose up --build`

## Stripe CLI local webhook verification
1. Login once:
   - `\"E:\\Chrome Downloads\\stripe_1.40.2_windows_x86_64\\stripe.exe\" login`
2. Start forwarding:
   - `\"E:\\Chrome Downloads\\stripe_1.40.2_windows_x86_64\\stripe.exe\" listen --forward-to http://localhost:8080/api/v1/billing/stripe/webhook`
3. Copy the printed signing secret into `.env`:
   - `STRIPE_WEBHOOK_SECRET=whsec_...`
4. Restart API:
   - `docker compose up --build -d api`
5. Trigger a test event:
   - `\"E:\\Chrome Downloads\\stripe_1.40.2_windows_x86_64\\stripe.exe\" trigger checkout.session.completed`
6. Verify:
   - wallet increases
   - `GET /api/v1/billing/stripe/webhook-events/{tenantId}` shows processed status

## Next implementation targets
- Social OAuth connectors (LinkedIn -> Meta -> X -> TikTok)
- Background publish worker and retry/backoff strategy
- Dashboard analytics cards and timelines
- Team invitation UX and role management screens
