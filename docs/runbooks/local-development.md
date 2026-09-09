# Running AkironCommerce locally

## The daily loop

Infrastructure runs in Docker; the services run from the IDE against it. There is no
Aspire AppHost and the app itself is not in the compose file — that is deliberate
(ADR-0015), so the only orchestration to learn is the one that ships (compose today,
Kubernetes in the Faz 8 lab).

```powershell
# 1. Infrastructure — once per machine boot, then leave it running
docker compose -f deploy/compose/docker-compose.infra.yml up -d --wait

# 2. The service — F5 in Visual Studio 2026, or:
dotnet run --project src/Services/Catalog/Akiron.Catalog.Api
```

`--wait` blocks until the healthchecks pass, so the command returning means the
databases are actually ready. Stopping is `down` (keeps data) or `down -v` (deletes it).

## Where everything is

| What | Address | Notes |
| --- | --- | --- |
| Catalog API | <http://localhost:5210> | F5 opens Scalar automatically |
| **Scalar (API explorer)** | <http://localhost:5210/scalar/v1> | Development only |
| OpenAPI document | <http://localhost:5210/openapi/v1.json> | For Postman/Insomnia import |
| Health | `/health` (liveness) · `/health/ready` (database reachable) | Outside tracing |
| **Jaeger** (traces) | <http://localhost:16686> | Service name `akiron-catalog` |
| Aspire dashboard | <http://localhost:18888> | Alternate telemetry pane |
| RabbitMQ UI | <http://localhost:15672> | `akiron` / `akiron_dev` (used from Faz 3) |
| Elasticsearch | <http://localhost:9200> | Used from Faz 3 |
| Mailpit | <http://localhost:8025> | Used from Faz 5 |
| **PostgreSQL** | `localhost:5433` | See below |

Every port binds to `127.0.0.1` only: the stack is not reachable from the network.

## PostgreSQL and pgAdmin

**The port is 5433, not 5432.** 5432 is deliberately left free so this stack can run
beside other local projects.

| Field | Value |
| --- | --- |
| Host | `localhost` |
| Port | `5433` |
| Username | `akiron` |
| Password | `akiron_dev` |
| Maintenance database | `akiron` |

These credentials are local-only and live in the compose file; deployments supply their
own through the environment.

### Registering the server in pgAdmin, once

1. Open pgAdmin 4. In the left tree right-click **Servers → Register → Server…**
2. **General** tab → Name: `AkironCommerce (docker)` — this is just a label.
3. **Connection** tab → fill in the table above, tick **Save password**.
4. Save. The server appears in the tree.

Everything then lives under
**Servers → AkironCommerce → Databases → `akiron_catalog` → Schemas → public → Tables**.

There are six databases, one per service (`akiron_identity`, `akiron_catalog`,
`akiron_order`, `akiron_payment`, `akiron_inventory`, `akiron_notification`). Only
`akiron_catalog` has tables so far; the others are empty until their service is built.
That separation is the point — no service may read another service's database
(ADR-0003).

### The three things worth knowing in the UI

- **See a table's rows:** right-click the table → *View/Edit Data* → *All Rows*. This is pgAdmin's `SELECT *` button.
- **Write SQL:** select the database, then *Query Tool* (`Alt+Shift+Q`). **F5** runs the statement under the cursor.
- **See a table's shape:** right-click → *Properties* for the GUI view, or run `\d products` equivalent SQL — the Columns and Constraints nodes under the table show indexes and foreign keys.

### Queries that are useful in this project

```sql
-- Which migrations have been applied?
SELECT * FROM __ef_migrations_history ORDER BY migration_id;

-- Money is two columns, not a type
SELECT sku, name, base_price_amount, base_price_currency FROM products;

-- Proof the Turkish collation is on the column
SELECT a.attname, c.collname
FROM pg_attribute a
JOIN pg_collation c ON c.oid = a.attcollation
WHERE a.attrelid = 'categories'::regclass AND a.attname = 'name';

-- Turkish sort order, which the default collation gets wrong
SELECT name FROM categories ORDER BY name;
```

### One warning about editing rows by hand

The value objects validate on the way *out* of the database as well as in. Editing a
`slug` to contain an uppercase letter, or a `sku` to contain a space, makes the row fail
to load with a 400 rather than sitting there quietly. That is intentional — but it means
hand-edits should respect the same rules the API does.

## Common situations

**Docker Desktop was closed.** Containers stop with it. Start Docker, then re-run the
compose command; data survives in named volumes.

**Port 5433 is busy.** Something else is on it — `docker compose ps` to see whether our
own stack is already up.

**`dotnet test` fails with "Docker is either not running".** The integration tests start
their own PostgreSQL through Testcontainers, so Docker Desktop has to be running even
though the tests do not use the compose stack.

**Schema looks stale.** In Development the API applies migrations at startup. To do it by
hand:

```powershell
dotnet ef database update --project src/Services/Catalog/Akiron.Catalog.Infrastructure/Akiron.Catalog.Infrastructure.csproj
```
