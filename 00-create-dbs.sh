
set -e

until pg_isready -U "$POSTGRES_USER" >/dev/null 2>&1; do
  sleep 1
done

createdb -U "$POSTGRES_USER" AuthService    2>/dev/null || true
createdb -U "$POSTGRES_USER" UserProgress   2>/dev/null || true
createdb -U "$POSTGRES_USER" PatternsMinIO  2>/dev/null || true