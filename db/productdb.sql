CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create table for plans
CREATE TABLE plan (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    icon_url VARCHAR(1000),
    is_active BOOLEAN DEFAULT TRUE,
    -- status VARCHAR(20) DEFAULT 'inactive', -- ACTIVE, INACTIVE, DELETED, ARCHIVED
    foreign_service_id varchar(200) NULL, -- ID of the plan in the foreign service like Stripe

    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create table for plan features
CREATE TABLE plan_features (
    plan_id INT PRIMARY KEY REFERENCES plan(id) ON DELETE CASCADE,
    max_messages INT, -- Maximum number of messages allowed per interval
    support_tier VARCHAR(100), -- Support tier for this price tier
    ai_support BOOLEAN DEFAULT FALSE -- AI support for this price tier
    -- TODO: other features
);

-- Create table for price tiers
CREATE TABLE price_tier (
    id SERIAL PRIMARY KEY,
    plan_id INT REFERENCES plan(id) ON DELETE CASCADE,
    price NUMERIC(10, 2) NOT NULL,
    duration INTERVAL NOT NULL,
    foreign_service_id varchar(200) NULL, -- ID of the plan in the foreign service like Stripe

    name VARCHAR(100) NOT NULL,
    description TEXT,
    icon_url VARCHAR(1000),
    status VARCHAR(20) DEFAULT 'INACTIVE', -- ACTIVE, INACTIVE, DELETED, ARCHIVED

    -- TODO: model bits to control things like refunds cancellation policies
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create table for client plan subscriptions

-- TODO: do I need another table for subscription status?

CREATE TABLE plan_subscription (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    price_tier_id INT REFERENCES price_tier(id) ON DELETE CASCADE,
    start_date TIMESTAMPTZ NOT NULL,
    end_date TIMESTAMPTZ NOT NULL, -- End date calculated based on duration
    status VARCHAR(20) DEFAULT 'inactive', -- active, cancelled, expired, trial, paused
    -- bits from other dbs
    client_id UUID NOT NULL, -- Client ID from userdb
    payment_confirmation_id UUID NOT NULL, -- Payment confirmation ID from paymentdb

    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create table for trail configurations
CREATE TABLE trial_offer (
    id SERIAL PRIMARY KEY,
    price_tier_id INT REFERENCES price_tier(id) ON DELETE CASCADE,
    duration INTERVAL NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    expiry_date TIMESTAMPTZ,

    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create table for trail offer usage
CREATE TABLE trial (
    id SERIAL PRIMARY KEY,
    trail_offer_id INT REFERENCES trial_offer(id) ON DELETE CASCADE,
    plan_subscription_id UUID REFERENCES plan_subscription(id) ON DELETE CASCADE,

    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE discount_code (
    id SERIAL PRIMARY KEY,
    price_tier_id INT REFERENCES price_tier(id) ON DELETE CASCADE,
    code VARCHAR(100) NOT NULL,
    value NUMERIC(10, 2) NOT NULL,
    duration INTERVAL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    expiry_date TIMESTAMPTZ,

    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE discount (
    id SERIAL PRIMARY KEY,
    discount_code_id INT REFERENCES discount_code(id) ON DELETE CASCADE,
    plan_subscription_id UUID REFERENCES plan_subscription(id) ON DELETE CASCADE,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);
