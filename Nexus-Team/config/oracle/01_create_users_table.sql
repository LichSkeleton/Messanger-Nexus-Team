-- ============================================================================
-- Script: 01_create_users_table.sql
-- Description: Creates the users table with constraints and indexes
-- Execution Order: 1
-- Dependencies: None
-- ============================================================================

-- Create users table
-- Примечание: Constraint UNIQUE автоматически создает индексы для username и email
CREATE TABLE users (
    id VARCHAR2(50) PRIMARY KEY,
    username VARCHAR2(50) NOT NULL UNIQUE,
    email VARCHAR2(255) NOT NULL UNIQUE,
    password_hash VARCHAR2(255) NOT NULL,
    display_name VARCHAR2(100) NOT NULL,
    avatar_url VARCHAR2(500),
    status NUMBER(1) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    last_seen_at TIMESTAMP,
    CONSTRAINT users_status_check CHECK (status IN (0, 1, 2, 3))
);

-- Create indexes for frequently queried columns
-- Индексы для username и email удалены, так как они создаются автоматически (UNIQUE)
CREATE INDEX idx_users_status ON users(status);
CREATE INDEX idx_users_created_at ON users(created_at);

-- Add comments to table and columns
COMMENT ON TABLE users IS 'Stores user account information';
COMMENT ON COLUMN users.id IS 'Unique identifier for the user';
COMMENT ON COLUMN users.username IS 'Username for login (unique)';
COMMENT ON COLUMN users.email IS 'Email address (unique)';
COMMENT ON COLUMN users.password_hash IS 'BCrypt hashed password';
COMMENT ON COLUMN users.display_name IS 'Display name shown to other users';
COMMENT ON COLUMN users.avatar_url IS 'URL to user avatar image';
COMMENT ON COLUMN users.created_at IS 'Timestamp when user was created';
COMMENT ON COLUMN users.updated_at IS 'Timestamp when user was last updated';
COMMENT ON COLUMN users.last_seen_at IS 'Timestamp when user was last seen online';

COMMIT;
EXIT;