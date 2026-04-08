DO
$$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postpebble_api') THEN
        CREATE ROLE postpebble_api LOGIN PASSWORD 'postpebble_api_dev_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postpebble_scheduler') THEN
        CREATE ROLE postpebble_scheduler LOGIN PASSWORD 'postpebble_scheduler_dev_password';
    END IF;
END
$$;

GRANT CONNECT ON DATABASE postpebble TO postpebble_api;
GRANT CONNECT ON DATABASE postpebble TO postpebble_scheduler;

GRANT USAGE ON SCHEMA public TO postpebble_api;
GRANT USAGE ON SCHEMA public TO postpebble_scheduler;
GRANT CREATE ON SCHEMA public TO postpebble_api;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO postpebble_api;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO postpebble_api;

GRANT SELECT, INSERT, UPDATE ON TABLE
    scheduled_posts,
    post_targets,
    linkedin_connections,
    credit_wallets,
    credit_transactions,
    credit_reservations
TO postpebble_scheduler;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO postpebble_scheduler;
