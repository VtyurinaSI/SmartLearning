SELECT 'CREATE DATABASE "AuthService"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'AuthService')\gexec;

SELECT 'CREATE DATABASE "UserProgress"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'UserProgress')\gexec;

SELECT 'CREATE DATABASE "PatternsMinIO"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PatternsMinIO')\gexec;