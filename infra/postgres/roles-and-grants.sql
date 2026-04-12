DO
$$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postpebble_api') THEN
        CREATE ROLE postpebble_api LOGIN PASSWORD 'postpebble_api_dev_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postpebble_scheduler') THEN
        CREATE ROLE postpebble_scheduler LOGIN PASSWORD 'postpebble_scheduler_dev_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postpebble_migrator') THEN
        CREATE ROLE postpebble_migrator LOGIN PASSWORD 'postpebble_migrator_dev_password';
    END IF;
END
$$;

GRANT CONNECT ON DATABASE postpebble TO postpebble_api;
GRANT CONNECT ON DATABASE postpebble TO postpebble_scheduler;
GRANT CONNECT ON DATABASE postpebble TO postpebble_migrator;

GRANT USAGE ON SCHEMA public TO postpebble_api;
GRANT USAGE ON SCHEMA public TO postpebble_scheduler;
GRANT USAGE, CREATE ON SCHEMA public TO postpebble_migrator;
REVOKE CREATE ON SCHEMA public FROM postpebble_api;

-- API user: full CRUD on all current and future tables
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO postpebble_api;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO postpebble_api;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO postpebble_api;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO postpebble_api;

-- Scheduler user: read/write on scheduler-related + read on media tables (current and future)
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO postpebble_scheduler;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO postpebble_scheduler;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE ON TABLES TO postpebble_scheduler;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO postpebble_scheduler;

-- Migrator: full privileges on all current and future tables
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postpebble_migrator;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postpebble_migrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON TABLES TO postpebble_migrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO postpebble_migrator;
