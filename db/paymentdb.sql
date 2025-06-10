CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE merchant_service (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,

    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE payment_confirmation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    merchant_service_id INT REFERENCES merchant_service(id) ON DELETE SET NULL,
    confirmation_code VARCHAR(300) NOT NULL,

    created_at TIMESTAMPTZ DEFAULT NOW()
);
