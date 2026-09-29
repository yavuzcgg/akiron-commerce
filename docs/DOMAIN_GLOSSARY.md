# Domain glossary

The business words this codebase uses, what each one means here, and the type that
holds it. When code and this page disagree, one of them is a bug.

The Turkish column is the word a dealer or a Logo ERP user would say; it is the term
to search for in conversations and in the owner's earlier B2B work.

## Catalog

| Term | Turkish | Meaning | In code |
| --- | --- | --- | --- |
| **Category** | Kategori | A flat grouping of products. No nesting yet. Cannot be deleted while it holds products. | `Category`, `CategoryId` |
| **Slug** | Slug / URL adı | The lower-case, hyphenated, URL-safe name of a category (`kis-lastikleri`). Unique, and never changes after creation, because links point at it. | `Slug` |
| **Product** | Ürün / Stok kartı | Something sellable, with one list price. | `Product`, `ProductId` |
| **SKU** | Stok kodu | The product's business identifier (`MIC-PRIMACY4-205-55-16`). Upper-cased on input so `abc-1` and `ABC-1` are the same SKU. Unique and immutable: orders, invoices and ERP records refer to it. | `Sku` |
| **Active / inactive** | Aktif / pasif | An inactive product still exists and keeps its history, but is not offered for sale. The alternative to deleting a product that has been ordered. | `Product.IsActive` |

## Pricing

Resolution order, from strongest to weakest: **agreed price → group discount → list
price**, then the **markup chain** on top, then **one rounding** at the very end.

| Term | Turkish | Meaning | In code |
| --- | --- | --- | --- |
| **Money** | Tutar | An amount *and* its currency, never one without the other. At most two decimal places; a third is rejected, not rounded. | `Money` |
| **Currency** | Para birimi | TRY, USD or EUR. There is no conversion between them yet (Faz 2, with its own ADR). | `Currency` |
| **List price** | Liste fiyatı / Satış fiyatı | The price on the product itself — what a buyer with no price group pays. | `Product.BasePrice` |
| **Price group** | Fiyat grubu | A class of buyer (dealer, wholesale, retail) with its own pricing. Declares one currency, fixed at creation; everything priced inside it uses that currency. Referred to by its code. | `PriceGroup`, `PriceGroupId` |
| **Price group code** | Grup kodu | The upper-case business name of a price group (`BAYI-A`, `TOPTAN`). Appears in URLs; immutable. | `PriceGroupCode` |
| **Group discount** | Grup iskontosu | A percentage off the list price for every product in the group, unless a product has an agreed price. The "dealers get 20% off" rule. | `PriceGroup.Discount`, `DiscountPercentage` |
| **Agreed price** | Anlaşmalı fiyat / Özel fiyat | A fixed price for one product in one group. **Wins over the group discount even when it is higher** — a negotiated price is the price. | `PriceListEntry` |
| **Price list** | Fiyat listesi | All agreed prices of one group. | `price_list_entries` |
| **Markup** | Kâr marjı / Üst bayi payı | A percentage *added* by a reseller. The opposite of a discount, which is why it is a separate type rather than a shared `Percentage`. | `Markup` |
| **Markup chain** | Bayi zinciri | The markups of each reseller level between the price group and the final buyer, at most five. **Compounding:** 10% + 10% is ×1.21, not ×1.20, because each level marks up what it paid. | `MarkupChain` |
| **Basis** | Fiyat kaynağı | Which rule produced the price before markups: `AgreedPrice`, `GroupDiscount` or `ListPrice`. Returned so a buyer can see *why* a price is what it is. | `PriceBasis` |
| **Quote** | Fiyat teklifi / Fiyat hesabı | The full breakdown for one product: list price, base price, basis, applied markups, final price. What the Order service will lock into an order (ADR-0012). | `PriceQuote`, `POST /api/v1/pricing/quote` |

## Not yet in the model

Named here so the words are not reused for something else in the meantime.

| Term | Turkish | Arrives with |
| --- | --- | --- |
| Dealer / buyer account | Cari / Bayi | Identity, Faz 2 — including which price group and markup chain a dealer has |
| Exchange rate | Kur | Faz 2, its own ADR |
| Campaign, brand discount | Kampanya, marka iskontosu | Not in v1 (see ROADMAP non-goals: Promotion) |
| Stock, reservation | Stok, rezervasyon | Inventory, Faz 4 |
