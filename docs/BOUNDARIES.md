# PostPebble Domain Boundaries

This project uses a modular monolith with strict ownership boundaries.

## Domain ownership

- Identity/Tenants owns: `users`, `tenants`, `memberships`
- Billing owns: `credit_wallets`, `credit_transactions`, `credit_reservations`, `stripe_webhook_events`
- Scheduler owns: `scheduled_posts`, `post_targets`, `scheduled_post_media`
- Media owns: `media_assets`
- Integrations owns: `linkedin_oauth_states`, `linkedin_connections`

## Dependency rules

- Scheduler must interact with billing via narrow contracts (`IReservationLedgerService`) instead of broad service internals.
- Authorization checks across domains should use `ITenantAccessService`.
- Runtime services (`api`, `scheduler`) do not run schema migrations; `migrator` performs all EF migrations.

## Operational boundaries

- API and Scheduler run with least-privilege database users.
- Migrator runs with schema-change permissions.
- Cross-domain workflows should be idempotent and reference-based.
