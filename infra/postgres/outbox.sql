\connect smurftracker

CREATE TABLE IF NOT EXISTS public.outbox (
    id SERIAL PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(255) NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'debezium') THEN
        CREATE ROLE debezium
        WITH LOGIN
        REPLICATION
        PASSWORD 'postgres';
    ELSE
        ALTER ROLE debezium
        WITH LOGIN
        REPLICATION
        PASSWORD 'postgres';
    END IF;
END
$$;

GRANT CONNECT ON DATABASE smurftracker TO debezium;
GRANT CREATE ON DATABASE smurftracker TO debezium;
GRANT USAGE ON SCHEMA public TO debezium;

DO $$
BEGIN
    IF to_regclass('public.outbox') IS NOT NULL THEN
        GRANT SELECT ON TABLE public.outbox TO debezium;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT FROM pg_publication WHERE pubname = 'outbox_publication'
    ) THEN
        CREATE PUBLICATION outbox_publication FOR TABLE public.outbox;
    END IF;
END
$$;