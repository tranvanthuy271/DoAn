#!/bin/bash
# Docker entrypoint init script — run SQL files in order
set -e

echo "=== Initializing gamedb ==="

# 1. Main schema + seed data
mysql -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" < /docker-entrypoint-initdb.d/sql/gamedb.sql
echo "  → gamedb.sql loaded"

# 2. Migrations (in order)
for f in /docker-entrypoint-initdb.d/sql/migration_*.sql; do
    if [ -f "$f" ]; then
        echo "  → Running $(basename "$f")..."
        mysql -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" < "$f" || echo "  ⚠ $(basename "$f") skipped (may already be applied)"
    fi
done

# 3. Additional SQL in sql/ subfolder
for f in /docker-entrypoint-initdb.d/sql/sql/*.sql; do
    if [ -f "$f" ]; then
        echo "  → Running $(basename "$f")..."
        mysql -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" < "$f" || echo "  ⚠ $(basename "$f") skipped"
    fi
done

echo "=== Database initialization complete ==="
