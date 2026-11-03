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

CREATE TABLE contosopay.app_clients (
    id VARCHAR(100) PRIMARY KEY,
    application_type VARCHAR(50) NULL,
    client_id VARCHAR(100) NOT NULL,
    client_secret TEXT NULL,
    client_type VARCHAR(50) NULL,
    concurrency_token VARCHAR(50) NOT NULL,
    consent_type VARCHAR(50) NULL,
    display_name VARCHAR(100) NULL,
    display_names TEXT NULL,
    json_web_key_set TEXT NULL,
    permissions TEXT NULL,
    post_logout_redirect_uris TEXT NULL,
    properties TEXT NULL,
    redirect_uris TEXT NULL,
    requirements TEXT NULL,
    settings TEXT NULL,
    merchant_id UUID NOT NULL,
    CONSTRAINT fk_app_clients_merchants FOREIGN KEY (merchant_id)
        REFERENCES contosopay.merchants (id)
);

CREATE UNIQUE INDEX ux_app_clients_client_id ON contosopay.app_clients (client_id);
CREATE UNIQUE INDEX ux_app_clients_merchant_id ON contosopay.app_clients (merchant_id);

CREATE TABLE contosopay.app_client_tokens (
    id VARCHAR(100) PRIMARY KEY,
    application_id VARCHAR(100) NULL,
    authorization_id VARCHAR(100) NULL,
    concurrency_token VARCHAR(50) NOT NULL,
    creation_date TIMESTAMPTZ NULL,
    expiration_date TIMESTAMPTZ NULL,
    payload TEXT NULL,
    properties TEXT NULL,
    redemption_date TIMESTAMPTZ NULL,
    reference_id VARCHAR(100) NULL,
    status VARCHAR(50) NULL,
    subject VARCHAR(400) NULL,
    type VARCHAR(150) NULL,
    CONSTRAINT fk_app_client_tokens_app_clients FOREIGN KEY (application_id)
        REFERENCES contosopay.app_clients (id)
);

CREATE UNIQUE INDEX ux_app_client_tokens_reference_id ON contosopay.app_client_tokens (reference_id);
CREATE INDEX ix_app_client_tokens_application_status_subject_type
    ON contosopay.app_client_tokens (application_id, status, subject, type);

GRANT ALL PRIVILEGES ON TABLE contosopay.app_clients TO appuser;
GRANT ALL PRIVILEGES ON TABLE contosopay.app_client_tokens TO appuser;
