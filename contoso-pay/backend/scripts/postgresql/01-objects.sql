CREATE DATABASE contoso;

\c contoso;

CREATE USER appuser WITH PASSWORD 'pass@word';

CREATE SCHEMA contosopay;

GRANT ALL PRIVILEGES ON DATABASE contoso TO appuser;
GRANT ALL PRIVILEGES ON SCHEMA contosopay TO appuser;
