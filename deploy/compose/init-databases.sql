-- Database-per-service bootstrap (ADR-0003).
-- Runs once on first container start (empty data volume).
-- One PostgreSQL instance, one database per service; the ownership boundary
-- is the database — cross-service access is forbidden (docs/DATA_OWNERSHIP.md).

CREATE DATABASE akiron_identity;
CREATE DATABASE akiron_catalog;
CREATE DATABASE akiron_order;
CREATE DATABASE akiron_payment;
CREATE DATABASE akiron_inventory;
CREATE DATABASE akiron_notification;
