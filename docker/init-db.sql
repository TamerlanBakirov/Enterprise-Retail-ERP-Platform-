-- Georgia ERP Database Initialization
-- Creates extensions and sets up locale support for Georgian

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schema for shared data
CREATE SCHEMA IF NOT EXISTS shared;

-- Grant permissions
GRANT ALL ON SCHEMA public TO erp_user;
GRANT ALL ON SCHEMA shared TO erp_user;
