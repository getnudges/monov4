#!/bin/bash

set -e

# Update these variables if necessary
POSTGRES_HOST=${POSTGRES_HOST:-postgres}
POSTGRES_PORT=${POSTGRES_PORT:-5432}
POSTGRES_USER=${POSTGRES_USER:-postgres}

# Wait for PostgreSQL to become available
echo "Waiting for PostgreSQL to become available..."
until pg_isready -h $POSTGRES_HOST -p $POSTGRES_PORT -U $POSTGRES_USER; do
  sleep 1
done

echo "PostgreSQL is available. Running script..."

psql -h $POSTGRES_HOST -p $POSTGRES_PORT -U $POSTGRES_USER -d postgres -c "DROP DATABASE IF EXISTS unad;"
psql -h $POSTGRES_HOST -p 5432 -U $POSTGRES_USER -d postgres -c "SHOW hba_file;"

echo "Script completed."
