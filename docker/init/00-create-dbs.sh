set -eux
: "${POSTGRES_USER:=postgres}"

echo ">> creating databases if missing"
createdb -U "$POSTGRES_USER" AuthService   2>/dev/null || true
createdb -U "$POSTGRES_USER" UserProgress  2>/dev/null || true
createdb -U "$POSTGRES_USER" PatternsMinIO 2>/dev/null || true

echo ">> applying schema to UserProgress"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d UserProgress \
     -f /docker-entrypoint-initdb.d/20-userprogress-db.psql

# apply optional PatternsMinIO schema if provided
if [ -f /docker-entrypoint-initdb.d/20-patternsminio.psql ]; then
  echo ">> applying schema to PatternsMinIO"
  psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d PatternsMinIO \
       -f /docker-entrypoint-initdb.d/20-patternsminio.psql
fi


echo ">> done"