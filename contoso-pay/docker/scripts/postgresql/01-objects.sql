CREATE DATABASE contoso;

\c contoso;

CREATE USER appuser WITH PASSWORD 'pass@word';

CREATE SCHEMA contosopay;

GRANT ALL PRIVILEGES ON DATABASE contoso TO appuser;
GRANT ALL PRIVILEGES ON SCHEMA contosopay TO appuser;

CREATE TABLE contosopay.merchants (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    document VARCHAR(14) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

GRANT ALL PRIVILEGES ON TABLE contosopay.merchants TO appuser;