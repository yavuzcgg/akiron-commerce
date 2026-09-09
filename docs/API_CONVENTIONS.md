# API conventions

What every AkironCommerce HTTP endpoint looks like. Decide once here; endpoints
follow. Anything below that deviates from a common .NET default says why.

## URLs and verbs

- `/api/v1/<resource>` — plural, lower kebab-case for multi-word segments (`/api/v1/price-groups`).
- The version is in the path. A breaking change to a response shape means `v2`, not a silent edit.
- Full REST verbs: `GET`, `POST`, `PUT`, `DELETE`. Actions that are not CRUD become a POST sub-resource (`/api/v1/products/{id}/reindex`), never a verb in the collection path.
- `/health` (liveness) and `/health/ready` (readiness) sit outside the version prefix and outside tracing.

| Situation | Status |
| --- | --- |
| Read succeeded | 200 |
| Created | 201 + `Location` header |
| Succeeded, nothing to return | 204 |
| Malformed input, or input the domain cannot represent | 400 |
| Unknown resource, or a reference to one | 404 |
| Collides with existing data or a constraint | 409 |
| Anything unhandled | 500 |

## JSON

- camelCase properties. Timestamps are ISO 8601 with an offset, always UTC.
- Typed ids serialise as plain uuid strings (`"01a085da-96dc-7a06-b5db-8acce0a697b0"`), never as an object wrapping a value.
- Money is flattened to `{ "amount": 4250.00, "currency": "TRY" }`. Amounts carry at most two decimals; a third is rejected, never rounded.

## Paging

Every list endpoint takes the same parameters and returns the same envelope.

```
GET /api/v1/products?page=1&pageSize=20&sort=name&direction=asc
```

```json
{ "items": [ ... ], "page": 1, "pageSize": 20, "totalCount": 183 }
```

- Defaults: `page=1`, `pageSize=20`. Maximum `pageSize` is **100**.
- **Out-of-range paging is rejected with 400, not clamped.** Quietly returning a different page size than asked for is how a client ends up believing it has read everything when it has not — the same reasoning that makes `Money` refuse a third decimal instead of rounding it.
- `totalCount` is the count for the *filtered* query, not the table. It costs a second `COUNT` per request, which is accepted because the admin tables draw page numbers. `totalPages` is left for the client to divide out rather than sent twice.
- `sort` is a whitelist per endpoint; anything outside it is a 400. Default order is newest first (`createdAt desc`).
- Paging is offset-based (`OFFSET`/`LIMIT`). Deep pages get slower as the offset grows. That is acceptable for admin tables; the storefront moves to Elasticsearch in Faz 3, and keyset paging is revisited there rather than guessed at now.

## Filtering, and why there is no text search

Filters are **exact matches** (`categoryId`, `isActive`, `sku`). There is deliberately
no `?search=` parameter.

PostgreSQL `ILIKE` is wrong for Turkish, measured against the running database:

```
'KIŞ LASTİĞİ' ILIKE '%kış%'  →  false
'Kış Lastiği' ILIKE '%KIŞ%'  →  false
```

The dotted and dotless i do not fold into each other, so a search box built on `ILIKE`
would quietly fail to find products that exist. A search that lies is worse than no
search. Real search arrives in Faz 3 with Elasticsearch and a Turkish analyzer
(ADR-0008).

SKU filtering is safe because SKUs go through the `Sku` value object, which upper-cases
them with invariant rules — `?sku=mich-1` finds `MICH-1`.

## Sorting Turkish text

`categories.name` and `products.name` carry `COLLATE "tr-TR-x-icu"`. Under the database
default (`en_US.utf8`) the Turkish letters sort after `z`:

```
default:      istanbul < zula < çakmak < öğle < ürün < ısı < şeker
tr-TR-x-icu:  çakmak < ısı < istanbul < öğle < şeker < ürün < zula
```

Any new column users will sort by needs the same collation.

## Errors

Every failure is `application/problem+json` (RFC 7807) plus two extensions from
ADR-0018: a stable `code` and the `params` a client needs to render its own message.
`detail` stays English — it is written for logs and bug reports, not for shoppers.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "code": "catalog.product.sku_conflict",
  "params": { "sku": "MICH-205-55-R16" },
  "detail": "A product with SKU 'MICH-205-55-R16' already exists.",
  "instance": "/api/v1/products",
  "traceId": "95381fa1834bf0147668655f489fa215"
}
```

`traceId` is the W3C trace id: paste it into Jaeger to see the request that produced it.

### Validation failures

```json
{
  "status": 400,
  "code": "validation_failed",
  "errors": {
    "slug": [ { "code": "catalog.slug.invalid_format", "message": "'Slug' must be lowercase letters, digits and single hyphens." } ]
  }
}
```

⚠️ **Deliberate deviation.** ASP.NET's `HttpValidationProblemDetails` types `errors` as
`Dictionary<string, string[]>` — messages only. Ours carries `{ code, message }` objects
so a client can translate per rule, which is the whole point of ADR-0018. The cost is
that `Results.ValidationProblem` cannot be used and the response is built by hand in
`ValidationEndpointFilter`.

### Error codes

Codes are `service.resource.reason`. They are **part of the public contract**: clients
branch on them and translate them, so renaming one is a breaking change.

The source of truth is
[`CatalogErrorCodes`](../src/Services/Catalog/Akiron.Catalog.Domain/Common/CatalogErrorCodes.cs);
the failures a handler can raise are built in
[`CatalogErrors`](../src/Services/Catalog/Akiron.Catalog.Application/Common/CatalogErrors.cs).

| Code | Status | Parameters |
| --- | --- | --- |
| `catalog.category.not_found` | 404 | `categoryId` |
| `catalog.price_group.not_found` | 404 | `priceGroupCode` |
| `catalog.price_list.entry_not_found` | 404 | `priceGroupCode`, `productId` |
| `catalog.product.not_found` | 404 | `productId` |
| `catalog.category.slug_conflict` | 409 | `slug` |
| `catalog.product.sku_conflict` | 409 | `sku` |
| `catalog.category.has_products` | 409 | — |
| `catalog.price_group.code_conflict` | 409 | `priceGroupCode` |
| `catalog.price_group.has_prices` | 409 | — |
| `catalog.slug.invalid_format` · `catalog.slug.too_long` | 400 | `slug` · `maxLength` |
| `catalog.sku.invalid_format` · `catalog.sku.invalid_length` | 400 | `sku` · `minLength`, `maxLength` |
| `catalog.money.negative_amount` · `catalog.money.too_many_decimals` · `catalog.money.unsupported_currency` | 400 | `amount` · `decimalPlaces` · `supported` |
| `catalog.price_group.code_invalid_format` | 400 | `code` · `minLength`, `maxLength` |
| `catalog.discount.out_of_range` | 400 | `discountPercentage` · `decimalPlaces` |
| `catalog.price.currency_mismatch` | 400 | `priceGroupCurrency`, `priceCurrency` |
| `catalog.text.required` · `catalog.text.too_long` | 400 | `field` · `maxLength` |
| `catalog.paging.page_out_of_range` · `catalog.paging.page_size_too_large` · `catalog.paging.sort_not_supported` | 400 | — |
| `validation_failed` | 400 | see `errors` |
| `unexpected_error` | 500 | — |

The `catalog.text.*` and value-object codes normally never reach a client: request
validation rejects bad input first. They fire when something bypasses the endpoint, so
treat them as a developer signal rather than a message to show.

## Prices

A price group fixes its **currency** at creation and every price inside it inherits that
currency — the request body for a price carries only an amount. A dealer who buys in
dollars is assigned a dollar group; converting between currencies is a separate concern
with its own exchange rates, designed in Faz 2 when Identity brings the dealer's own
currency.

Setting a price is a `PUT`, and it means it: **201** the first time, **200** every time
after, whatever order concurrent callers arrive in. That guarantee comes from a single
`INSERT ... ON CONFLICT DO UPDATE` rather than a read followed by a write, because the
gap between those two steps is one that two callers can both fall into.

Delete behaviour differs on purpose, and the difference is the rule:

| Deleting | Effect |
| --- | --- |
| A product | Its prices go with it — a price for a product that no longer exists has nothing left to mean |
| A price group holding prices | Refused with 409 — wiping a list of agreed dealer prices must be deliberate |
| A category holding products | Refused with 409 |

## Discovering the API

- Scalar UI: `/scalar/v1` (Development only).
- OpenAPI document: `/openapi/v1.json`.
