# AkironCommerce — Teknoloji & Mimari Planı (v1.0, 2026-08-27)

> **Kaynak plan dokümanı (Türkçe).** Faz 0 ile resmileşen güncel/canonical içerik:
> [SYSTEM_ARCHITECTURE.md](SYSTEM_ARCHITECTURE.md) · [ROADMAP.md](ROADMAP.md) · [adr/](adr/) · [/AGENTS.md](../AGENTS.md).
> Çelişki durumunda canonical dokümanlar kazanır; bu dosya tarihi bağlam olarak korunur.

## Bağlam

AkironCommerce: Yavuz'un portfolyo + öğrenme odaklı, satışa çıkarılabilir kalitede mikroservis B2B/B2C e-ticaret platformu. Klasör boş, sıfırdan başlıyoruz (Temmuz'daki repo veri kaybında gitti, GitHub'da da yok). Temmuz 2026 kilitli kararları **baz alınıp modernize edildi**; bugünkü yeni kararlarla birleştirildi.

**Bugünkü kararlar (2026-08-26):**
- Temmuz kararları baz + modernize ✅
- 4 "fantazik" öğrenme hedefi kesin dahil: **gRPC, Elasticsearch, Event Sourcing (Order), Kubernetes (k3s)** ✅
- VPS'e deploy: **sığar, şartlı** (bugün SSH ile ölçüldü — §6) ✅
- Frontend: bu sefer **lean değil, gerçek büyük commerce sitesi** — Next.js TS, yavaş yavaş ✅
- Repo: **public** (akiron-seo gibi açık geliştirme) ✅

**Ek kararlar (2026-08-27):**
- **Performans birinci sınıf hedef** ("site uçacak"): her faz demosuna k6/API p95 hedefi, frontend fazlarına Core Web Vitals bütçesi eşlik eder; cache stratejisi (Redis + ES + HTTP caching) fazların doğal parçası.
- **Bulut (Azure/AWS) v1 kapsamında değil** — mimari 12-factor/env-config kaldığı için kapı açık; iyi ilerlenirse 2027+ backlog maddesi.
- **Repo yönetişimi: AGENTS.md anayasa sistemi** (§11) — repo kendini açıklar, AI'lar pattern icat edemez.

**Temel ilke — dikey dilim önce:** genişlemesine iskelet kurmak (önce onlarca boş proje, sonra doldurma) bu tür projelerin bilinen ölüm şeklidir → motivasyon uçurumu. Bu planda tersi geçerli: **servis #2 açılmadan önce servis #1 uçtan uca demo edilebilir olacak** (endpoint + migration + test + Dockerfile + yeşil CI).

---

## 1. Servis Haritası (6 servis — 7.si YOK)

| Servis | Veri | CQRS | Not |
|---|---|---|---|
| **Identity** | PG `akiron_identity` | Hayır | Kullanıcı, bayi, alt-bayi ağacı, price group ataması, yetki bayrakları, JWT (RS256/ES256 + JWKS), dönen refresh token aileleri (akiron-seo deseni) |
| **Catalog** | PG `akiron_catalog` + **ES index** | Hayır | Ürün, kategori, price group bazlı fiyat listeleri, markup zinciri. ES index'inin ve indexer'ın sahibi |
| **Order (+Cart)** | PG `akiron_order` (**Marten** event store) + **Redis** (cart) | Evet | Cart = Redis state-based; Order aggregate = event-sourced; **saga burada yaşar** |
| **Payment** | PG `akiron_payment` | Evet | `IPaymentProvider`, önce PayTR; webhook; idempotency lab'ı |
| **Inventory** | PG `akiron_inventory` | Evet | Stok, rezervasyon; stok race-condition lab'ı |
| **Notification** | PG `akiron_notification` | Hayır | İnce: consumer + e-posta şablonları. EN SON yapılır, **max 2 proje** |

**Non-goal (v1'de KESİNLİKLE yok):** Shipping, Review, Promotion servisleri; multi-tenant iddiası; pazaryeri entegrasyonu; mobil app. Sadece backlog listesi.

### İletişim kuralı: "gRPC senkron sorgu sorar; broker asenkron command/event taşır"

Senkron gRPC yalnızca kullanıcı isteğini tamamlamak için gereken **read-only** sorgularda. Tüm state değişimi ve iş akışları RabbitMQ/MassTransit'ten geçer ve ikiye ayrılır: **command** = tek alıcıya yönerge (`ReserveStock`, `ProcessPayment`), **event** = olmuş bir olgunun ilanı, n alıcı (`StockReserved`, `PaymentCompleted`). Örnek üçlü: `GetPricingSnapshot` → gRPC sorgusu · `ReserveStock` → RabbitMQ command · `StockReserved` → RabbitMQ event.

| Çağıran → Çağrılan | RPC | Neden |
|---|---|---|
| Order → Catalog | `GetPricingSnapshot(items, priceGroupId)` | Checkout fiyatı **sunucu otoriter** — JWT claim'e güvenilmez, Catalog yeniden hesaplar |
| Order → Inventory | `CheckAvailability(sku[], qty[])` | Sepet/checkout önizleme ipucu. Deadline 200ms, fallback: "bilinmiyor, geç — saga doğrular" (bilinçli resilience dersi) |
| Herkes → Identity | `GetDealerPermissions(dealerId)` | Yetkiler sunucu tarafı (kilitli karar). Redis cache TTL 5dk + `DealerPermissionsChanged` event'iyle invalidation |

- gRPC client'ları `Microsoft.Extensions.Http.Resilience` (Polly v8) ile sarılır: timeout, retry, circuit breaker.
- **ADR kuralı:** saga adımı içinde gRPC yok; gRPC zinciri (A→B→C) yok. Dış dünya sadece YARP üzerinden REST/JSON.

### Elasticsearch (Catalog'un içinde)
- Postgres = kayıt sistemi; ES = **atılabilir projeksiyon**.
- Catalog `ProductUpserted/ProductDeleted/PriceListChanged` event'lerini RabbitMQ'ya basar; Catalog içindeki "Indexer" modülü (MassTransit consumer) ES'e yazar.
- `GET /catalog/search`: Türkçe analyzer, full-text, facet (kategori/marka/özellik), price-group bazlı fiyat.
- `reindex` konsol komutu: Postgres'ten index'i sıfırdan kurar (kurtarma + demo).

### Saga (MassTransit state machine, Order içinde)
`OrderSubmitted` → `ReserveStock`(Inventory) → `StockReserved` → `ProcessPayment`(Payment) → `PaymentCompleted` → `OrderConfirmed` → Notification e-posta. Kompanzasyon: `PaymentFailed` → `ReleaseStock`; `StockReservationFailed` → `OrderRejected`. Saga state: **EF Core saga repository** (aynı DB'de ayrı tablolar — Marten değil; Marten=aggregate, EF=saga çizgisi net kalsın).

### Race-condition lab'ları (her biri k6 script'i + DEVLOG yazısıyla biten milestone)
1. **Stok rezervasyonu** (Inventory): naive (oversell'i göster) vs Postgres optimistic (`xmin`) vs `SELECT FOR UPDATE` vs Redis `SET NX PX` — k6 altında karşılaştır, Redlock tartışmasını yaz.
2. **Idempotent ödeme** (Payment): idempotency-key (Redis SETNX + TTL, fallback PG unique index), MassTransit inbox dedupe, PayTR webhook replay simülasyonu.
3. **Cart eşzamanlılığı** (Order/Cart): iki sekme aynı sepeti değiştirir — Redis Lua CAS / versiyon alanı, ETag + `If-Match`.

---

## 2. Event Sourcing: **Marten 9.x** (sadece Order aggregate)

- **Neden Marten:** zaten işlettiğin PostgreSQL üstünde çalışır (VPS'e yeni stateful konteyner yok), `FetchForWriting` ile hazır optimistic concurrency (race öğrenme hedefini besler), `mt_events` tablosu okunabilir — öğrenen için şeffaf. EventStoreDB/Kurrent: ekstra stateful konteyner + ops yükü → elendi. Sürüm: **9.x** (2026-08-27 itibarıyla güncel: 9.30.0; .NET 10 uyumu Faz 5 spike'ında teyit edilir).
- **Önce 1 haftalık el-yapımı spike** `/labs/event-store-spike`: append-only tablo, `(stream_id, version)` unique index, naive projector. Sonra çöpe at, Marten'e geç — "sihir" mekaniğe dönüşür.
- **Kapsam disiplini:** SADECE Order aggregate event-sourced (`OrderPlaced, OrderPriceLocked, OrderStockReserved, OrderPaymentRecorded, OrderConfirmed, OrderCancelled`). Cart değil, saga state değil.
- **Marten + MassTransit birlikteliği:** yazma = MediatR handler → `session.Events.Append` → tek PG transaction (event'ler zaten state'in kendisi — bu tarafta atomiklik bedava). Yayınlama = **"Marten Event Relay"**: async daemon'da koşan bir subscription, commit edilmiş event'leri checkpoint'ten sıralı okuyup MassTransit `IPublishEndpoint` ile basar. ⚠️ Bu relay **outbox-benzeridir ama outbox'ın kendisi değildir**: RabbitMQ publish'i PG commit'iyle atomik DEĞİL — garanti **at-least-once**, aynı event iki kez basılabilir → **idempotent consumer zorunlu** (MassTransit inbox / messageId dedupe). Bu ayrımın kendisi projenin distributed-systems derslerinden biri. Diğer servisler (Payment/Inventory) standart **MassTransit EF Core outbox/inbox** kullanır → iki stili de karşılaştırmalı öğrenirsin (mülakat altını).
- **Çıkış rampası (ADR'a yazılacak):** Marten+MassTransit sürtünmesi ~2 haftayı aşarsa → state-based Order + EF outbox'a dön, saga aynen kalır.

---

## 3. Teknoloji Yığını

| Alan | Seçim | Sürüm | Not |
|---|---|---|---|
| Runtime | .NET 10 (LTS), C# 14 | — | Minimal API, endpoint-module (akiron-seo konvansiyonu), `.slnx` |
| ORM | EF Core + Npgsql | 10.x | CQRS servislerde sıcak okuma yollarında Dapper |
| Event store | Marten | **9.x** (güncel: 9.30.0) | Sadece Order; .NET 10 uyumu Faz 5 spike'ında teyit |
| DB | PostgreSQL | 16-alpine | Tek konteyner, DB-per-servis (`init-databases.sql`) |
| Mesajlaşma | RabbitMQ + MassTransit | RMQ 4.1.x, **MT 8.5.x (varsayılan)** | ⚠️ v9 ticari (Massient) ve **v8 EOL: 2026 sonu**. <$1M ciroya %100 indirimli v9 lisansı var. Gerçek ADR: "v8 OSS freeze vs v9 ücretsiz lisans" — karar kapısı Faz 5 başı (§8, §9.3) |
| Mediator | MediatR | **12.5.x SABIT** | ⚠️ v13+ ticari — v12 Apache, ADR'la sabitle |
| Gateway | YARP | 2.3.x | REST edge, rate limiting |
| gRPC | Grpc.AspNetCore | 2.7x | Proto'lar `Akiron.Contracts/protos` |
| Arama | Elasticsearch | **9.5.x** (Ağu 2026: 9.5.2) | Yeni repo = güncel major; Faz 3 başında mini spike: `Elastic.Clients.Elasticsearch` 9.x + Türkçe analyzer + heap teyidi, sorun çıkarsa 8.19.x'e inilir. Docker imajı ELv2 (ücretsiz basic), kaynak lisansında AGPL seçeneği; **heap her yerde 512MB**. OpenSearch = belgelenmiş B planı |
| Cache/kilit | Redis | 8.x — **üçlü lisans (AGPLv3/SSPLv1/RSALv2), bilinçli tercih: AGPLv3** | Kullanım haritası: katalog cache, yetki cache, distributed lock lab, idempotency, cart |
| İzleme | OpenTelemetry + **Jaeger v2** + **Aspire dashboard (standalone konteyner)** | OTel 1.12+ | OTLP **1. dilimden itibaren**. Seq YOK, Prometheus+Grafana → k8s fazına ertelendi |
| Log | Serilog | 9.x | Console JSON + OTLP; RFC 7807 |
| API docs | MS OpenAPI + Scalar | — | Swashbuckle yerine |
| Validasyon | FluentValidation | 12.x | |
| Test | xUnit v3 + **Testcontainers 4.x + Respawn 6.x** | — | Gerçek PG/Redis/RMQ/ES; collection başına konteyner + Respawn reset |
| Yük testi | k6 | 1.x | Race lab'ların kanıt aracı, `/labs` |
| .NET Aspire | **AppHost: HAYIR — Dashboard: EVET** | 9.x | AppHost = compose+k8s yanında 3. orkestrasyon modeli olurdu; hedeflerin hiçbirine ship etmiyor. ADR'la |
| Frontend | Next.js 16, React 19, TS strict, Tailwind 4, shadcn/ui, TanStack Query 5, Zustand 5, RHF+zod, next-intl, Vitest+RTL | — | §5 |
| Local k8s | **k3d** (Docker'da gerçek k3s) | 5.x | Faz 8; kind = fallback |
| Dev e-posta | Mailpit | latest | Notification testi |
| CI | GitHub Actions + GHCR | — | Public repo = sınırsız dakika |

---

## 4. Çözüm Yapısı (monorepo, isimlendirme: `Akiron.*`)

```
AkironCommerce/
├─ CLAUDE.md, README.md, AkironCommerce.slnx
├─ Directory.Build.props          # TreatWarningsAsErrors, nullable, analyzers
├─ Directory.Packages.props       # CPM — lisans pinleri burada yaşar
├─ docs/                          # SYSTEM_ARCHITECTURE.md, ROADMAP.md, DEVLOG.md, adr/
├─ deploy/
│  ├─ compose/                    # infra.yml, full.yml, vps override
│  └─ k8s/                        # Faz 8'e kadar BOŞ — önceden manifest yazma!
├─ src/
│  ├─ BuildingBlocks/             # başlangıçta TAM 3 proje:
│  │  ├─ Akiron.SharedKernel/     # Result<T>, Error, Entity, AggregateRoot, ValueObject, IDomainEvent, typed ID
│  │  ├─ Akiron.ServiceDefaults/  # AddAkironDefaults(): Serilog+OTel+health+RFC7807
│  │  └─ Akiron.Contracts/        # integration event record'ları + /protos
│  ├─ Gateway/Akiron.Gateway/
│  └─ Services/<Name>/            # Domain/Application/Infrastructure/Api + tests/
├─ frontend/                      # Next.js (pnpm)
└─ labs/                          # event-store spike, k6, race harness'ları
```

**Anti-iskelet kuralları (AGENTS.md anayasasına aynen girecek — §11):**
1. Başlangıçta tam 3 BuildingBlock. Yeni ortak proje = aynı kod **2 serviste** tekrarlanmış olmalı. `Akiron.Common` çöp kutusu YASAK.
2. **Önceki servis uçtan uca demo edilebilir olmadan** (endpoint + migration + Testcontainers testi + Dockerfile + yeşil CI) yeni servis klasörü AÇILMAZ.
3. Her oluşturulan `.csproj`, aynı faz içinde geçen bir test + erişilebilir endpoint'e sahip olmalı — boş proje yasak.
4. 4 katman bir tavandır, kota değil: Notification max 2 proje.
5. CQRS/MediatR sadece Order/Payment/Inventory'de (kilitli).

---

## 5. Frontend (büyük düşün, dilim dilim yap)

Tek Next.js 16 App Router app; route group'lar: `(storefront) / (dealer) / (admin) / (auth)`. **Her backend fazının görünür bir frontend çıktısı olacak** — motivasyon motoru bu.

- **Veri katmanı:** SEO-kritik storefront sayfaları RSC/server-fetch (docker ağı içinden gateway'e); tüm client etkileşimi TanStack Query 5 (optimistic update).
- **Cart:** sunucu-otoriter (Order servisinde Redis); client TanStack Query ile aynalar. Zustand SADECE geçici UI state (drawer, checkout adımı, filtre paneli). Anonim cart = HttpOnly `cart-id` cookie, login'de merge (merge'ün kendisi mini concurrency lab'ı).
- **Auth: BFF-lite (Next.js Route Handlers).** akiron-seo cookie deseninin uyarlaması: HttpOnly/Secure/SameSite=Lax session cookie; 15dk access JWT + dönen refresh ailesi Next sunucu tarafında; **token browser JS'ine asla inmez**. Identity RS256/ES256 + JWKS → her servis lokal doğrular. Middleware `(dealer)/(admin)`'i rol claim'iyle kapılar; ince yetki kontrolü sunucuda.
- **UI:** Tailwind 4 `@theme` token'ları + shadcn/ui; `frontend/DESIGN_SYSTEM.md`. Storybook YOK — local `/dev/components` playground route'u.
- **i18n:** next-intl **1. günden** (string'ler mesaj dosyalarında), TR varsayılan, EN tembel doldurulur.
- **Admin:** TanStack Table 8 + RHF+zod; **önce read-only admin** (sipariş timeline, ürün listesi), mutasyonlar sonra.
- Playwright e2e → hardening fazına.

---

## 6. Local + VPS Topolojisi

**Local günlük döngü:** `deploy/compose/docker-compose.infra.yml` → PG 16, Redis 8, RabbitMQ 4 (mgmt UI), ES (512MB heap, security off), Jaeger v2, Aspire dashboard, Mailpit — hepsi healthcheck'li. Servisler IDE/`dotnet run` ile bu altyapıya karşı koşar. `docker-compose.full.yml` = tam demo.

**Kubernetes: local'de k3d, VPS'te ASLA.** 13GB boş disk, containerd'nin Docker'dan ayrı image store'unu + etcd/sqlite churn'ünü kaldırmaz (swap zaten 1.7/2GB). Karar: **k8s local, VPS compose** (ADR).

**VPS verdiği (yavuz-prod-1, CX33 — 2026-08-26/27 SSH ile ölçüldü: RAM 4.2/7.6GB dolu, ~3.4GB boş; swap 1.7/2GB):**
- ✅ **Sığar, TEK ŞARTLA:** AkironCommerce çalışırken **FormReader VLM sidecar KAPALI** (`OCR_VLM=0` → 2.2GB boşalır, kullanılabilir RAM ~5.6GB olur; 3.4GB ile sağlıklı sığmaz).
- ✅ **Disk sorunu ÇÖZÜLDÜ (2026-08-27):** doluluğun kaynağı 3 proje değildi — sunucudaki her `--build`'li deploy'un aylardır biriktirdiği **BuildKit build cache**'iydi (containerd store'da ~44GB, 252 kayıt). `docker builder prune -af` + dangling image prune uygulandı: **~47GB geri geldi, disk %83 → %25 (55GB boş)**; 12 konteynerin hiçbiri etkilenmedi. ES watermark riski (%85/%95) fiilen yok. Kalıcı önlem (Faz 7): haftalık `builder prune` cron'u + compose log cap.
- **Yayın stratejisi:** günlük geliştirme TAMAMEN local (Faz 0–6); canlıya çıkış Faz 7'de (`akiron.yavuzcelik.com`). Opsiyon: Faz 3 sonunda (arama demosu hazırken) erken bir canlı preview alınabilir — karar o gün.
- Compose `portfolio_net`'e katılır, host portu YOK; Caddy: `akiron.yavuzcelik.com` → Next.js, `api.akiron.yavuzcelik.com` → YARP. Log cap (`max-size: 10m, max-file: 3`) + haftalık prune cron.
- Yeni disk ayak izi: ~5–6GB (ES imajı ~1.4GB dahil).

**RAM bütçesi (cap'li tam yığın):** PG 400–500MB (`shared_buffers=256MB`) · Redis 60–100 (`maxmemory 128mb`) · RMQ 250–350 (watermark 400MB) · ES 900–1100 (heap 512) · 6 .NET servisi 750–1050 (`GCHeapHardLimit` ~200MB/servis) · YARP 100–150 · Next.js 150–250 · Jaeger 200–300 (opsiyonel, ilk atılacak) ≈ **2.9–3.8GB** → VLM kapalıyken ~1.5–2GB pay kalır. Darlıkta önce Jaeger düşer, ES asla (demo o).

---

## 7. Fazlı Yol Haritası (dikey dilim önce; haftada 6–10 saat varsayımı)

| Faz | Süre | İçerik | Demo çıktısı |
|---|---|---|---|
| **0 — Bootstrap** | 1–2 hf (Eyl) | `git init` GÜN 1 + GitHub public repo + aynı gün push. Yönetişim çekirdeği (§11 Katman 0: AGENTS.md anayasa + minik CLAUDE.md + ARCHITECTURE.md + DATA_OWNERSHIP + ADR ilk parti + devlog + 2 skill), CPM props, infra compose, CI iskeleti. **Catalog dışında servis klasörü YOK.** | Repo online, `compose up` sağlıklı, CI yeşil |
| **1 — Catalog dilimi** | 4–5 hf (→ Eki ortası) | Catalog uçtan uca: domain (price group + markup zinciri), EF+migration, endpoint'ler, Testcontainers, Dockerfile, path-filtered CI, **OTel→Jaeger 1. dilimden** | Ürün/kategori API + Jaeger'da trace |
| **2 — Identity + Gateway + FE kabuğu** | 5–6 hf (→ Kas sonu) | JWT+JWKS+refresh aileleri, bayi claim'leri, YARP, Next.js kabuk + storefront listeleme, BFF auth, **ilk gRPC** (`GetDealerPermissions`) + Redis cache | Login → **bayiye özel fiyatlı** storefront |
| **3 — Arama** | 4 hf (→ Ara 2026) | MassTransit girer: `ProductUpserted` → ES indexer, faceted search API+UI, Türkçe analyzer, reindex | **Dürüst Aralık 2026 milestone'u: facet'li arama sayfası** |
| **4 — Cart + Inventory + gRPC + race lab 1&3** | 6–8 hf (Oca–Şub 27) | Redis cart + cart lab'ı; Inventory + stok lab'ı (k6 kanıtlı); `CheckAvailability`/`GetPricingSnapshot` + resilience | Sepet→checkout önizleme; **k6 raporu: oversell yok** |
| **5 — Saga + Event Sourcing + Payment + Notification** | 8–10 hf (Mar–May 27) | MassTransit lisans ADR'ı yeniden açılır (v8 EOL geçmiş olacak); `/labs` spike (1 hf) → Marten Order + Event Relay; saga + kompanzasyon; PayTR sandbox + idempotency lab'ı; Notification (ince) + Mailpit | Tam checkout: öde→onay→e-posta; **event stream'den admin sipariş timeline'ı**; saga ortasında servis öldürme demosu |
| **6 — Frontend derinleşme** | 6–8 hf (May–Haz 27) | Bayi portalı (alt bayi, markup yönetimi), checkout cilası, admin mutasyonları, EN locale | "Gerçek commerce sitesi" |
| **7 — VPS deploy + hardening** | 3–4 hf (Tem 27) | Disk temizliği, VLM-off runbook, cap'li compose + Caddy, VPS'e k6, güvenlik geçişi | **Canlı demo: akiron.yavuzcelik.com** |
| **8 — Kubernetes (local k3d)** | 4–6 hf (Ağu–Eyl 27) | Manifest/kustomize, probe, resource limit, HPA demosu, kube-prometheus-stack | Portfolyo için cluster demo + yazı |

**Dürüstlük:** Aralık 2026 = Faz 3 sonu (auth + bayi fiyatlama + arama — tek başına güçlü portfolyo). Tam vizyon (büyük frontend + k8s) **2027 ortası-sonu**. Bu README'nin status tablosunda saklanmadan yazılır.

---

## 8. Docs & Süreç

- **ADR'lar 0001'den yeniden başlar.** İlk parti: Temmuz kararlarının yeniden kaydı + yeniler: gRPC kriteri ("senkron sorgu vs asenkron command/event"), Marten-over-ESDB (+Wolverine gerilimi +çıkış rampası), ES 9.5.x-over-OpenSearch (+lisans notu), Aspire-AppHost-yok, **MassTransit: v8 OSS freeze vs v9 ücretsiz lisans** (v8 EOL 2026 sonu; varsayılan v8.5.x pin — gerekçe: public repo'yu klonlayan herkes lisanssız çalıştırabilsin; karar kapısı Faz 5 başında yeniden açılır) + MediatR v12 pini, Redis üçlü lisanstan AGPL seçimi, k8s-local/VPS-compose, `Akiron.*` isimlendirme, public repo.
- **docs/devlog/YYYY-AA.md (ay başına dosya, tek dev dosya değil):** her oturum bir kayıt (tarih / hedef / yapılan / öğrenilen / karar / problem / sonraki / commit). Race-lab yazıları blog kalitesinde.
- **AGENTS.md (anayasa) + minik CLAUDE.md (`@AGENTS.md` import):** sert kurallar tek yerde, anayasada yaşar — detay §11. Git disiplini korunur: *her anlamlı adımda commit, her oturum sonunda push* (Temmuz veri kaybı dersi); push daima Yavuz'un onayıyla, `/finish-session` ritüelinin son adımı olarak.
- **Git:** conventional commits, trunk-based, faz başına tag (`faz-1`...). Repo **public**.
- **CI:** tek reusable `dotnet-service.yml` (`workflow_call`: path input → build warnings-as-errors → Testcontainers test → GHCR imaj) + `paths:` filtreli servis workflow'ları; `ci-web.yml` (pnpm lint/typecheck/vitest/build); concurrency cancel + cache. Deploy = manuel `workflow_dispatch` → SSH → `git pull && compose up -d`.
- README: akiron-seo'daki **dürüst feature-status tablosu** geleneği (Done/Partial/Planned/Non-goal).

---

## 9. Riskler & Kesinti Sırası

1. **Yığılma riski = 1 numaralı tehdit.** Mikroservis+saga+gRPC+ES+event sourcing+k8s+büyük frontend = solo part-time için 2 yıllık kapsam. Yapısal önlem: dikey dilim, demo'lu fazlar, k8s en sona. **Hayat araya girerse kesinti sırası:** (1) k8s → 2028, suçluluk yok. (2) Prometheus/Grafana. (3) Admin mutasyon genişliği (read-only kalır). (4) EN locale. (5) **Event sourcing** — Faz 4 sonunda karar kapısı: enerji düşükse state-based Order + EF outbox, saga aynen kalır (asıl ders saga'da; ES-in-Order garnitür). gRPC ve Elasticsearch KESİLMEZ — CV değerine göre en ucuz ikisi.
2. **VPS diski — ÇÖZÜLDÜ (2026-08-27):** BuildKit cache prune'landı, %83 → %25 (bkz. §6). Kalan iş: haftalık prune cron + log cap (Faz 7). **VLM kapalı şartı (RAM) geçerliliğini korur.**
3. **Lisans kayması:** MassTransit v9 ticari + **v8 EOL 2026 sonu** (frozen ama Apache-2.0 — çalışmaya devam eder, güncelleme/güvenlik yaması gelmez). <$1M ciro için %100 indirimli v9 lisansı mevcut; karşılığı: public repo'yu klonlayanın da lisans alması gerekir (demo sürtünmesi). Varsayılan: v8.5.x pin, Faz 5 başında ADR yeniden açılır. MediatR v13 ticari → v12.5.x pin. İkisi de Directory.Packages.props + ADR ile kayıt altında.
4. **Marten+MassTransit az bilinen ikili** (Marten'in doğal eşi Wolverine) — sürtünme bütçele, çıkış rampası ADR'da.
5. **Motivasyon uçurumu (boş-iskelet tuzağı):** en koruyucu kural — *boş proje yasak*; her csproj aynı fazda test + endpoint'e kavuşur.

---

## 10. AI Çalışma Protokolü (Claude Code + Codex)

Bu proje "AI'ın yazdığı ürün" değil, "AI ile çalışarak dönüşen mühendis" projesi. **Roller sabit (2026-08-27, Yavuz'un kararı):**

- **Claude Code = kodu yazan.** Günlük geliştirme ortağı: tasarım tartışması, implementasyon, test, migration, refactor — kod Claude ile yazılır.
- **Codex = plan/tasarım danışmanı + ikinci göz.** Plan ve mimari review, büyük feature'larda tasarım itirazı, kritik diff'lerde staff-engineer code review ("race condition, transaction boundary, idempotency, test açığı ara" brief'iyle). Codex kod yazmaz; aynı işi iki AI'a paralel yazdırıp karşılaştırmak YOK.
- **Feature akışı:** Yavuz problemi okur → Claude ile tasarım tartışması → **Yavuz data flow'u KENDİ cümleleriyle açıklar** → Claude implementation planı → (büyük feature'da Codex plan review) → Yavuz onaylar/değiştirir → Claude implement + test → kritik işlerde Codex diff review → Yavuz diff'i okur → **en az bir parçayı kendisi refactor eder** → DEVLOG + commit.
- **Altın kural: AI'ın yazdığı kodu açıklayamıyorsan merge YOK.** Örn. `FetchForWriting<Order>(orderId)` görünce: ne yapıyor, expected version nereden geliyor, iki eşzamanlı istek aynı Order'ı değiştirirse ne olur, PostgreSQL'de hangi SQL üretiliyor, concurrency exception ne zaman atılıyor — cevaplanmadan devam edilmez.
- **Önce kır, sonra düzelt:** lab'larda sıra hep bilerek-hatalı implementasyon → ölç (k6) → düzelt → tekrar ölç → DEVLOG'a yaz. Mülakatta "idempotency nedir"e ezber değil, "PayTR webhook'unu replay ettim, duplicate transaction ürettim, şu üç katmanla çözdüm" cevabı verilir.

---

## 11. Repo Yönetişimi — AGENTS.md Anayasa Sistemi

Amaç: **repo kendi kendini açıklasın; AI'lar (Claude Code + Codex) aylar sonra bile kendi pattern'ini icat edemesin.** Mekanikler resmi dokümantasyondan doğrulandı (2026-08-27): Claude Code `AGENTS.md`'yi doğrudan OKUMAZ → minik `CLAUDE.md` onu `@AGENTS.md` ile import eder (import'lar launch'ta yüklenir, 4 seviyeye kadar). `.claude/rules/*.md` + `paths:` frontmatter GERÇEK bir özellik: path'siz kurallar her zaman, path'li kurallar eşleşen dosyaya dokununca on-demand yüklenir. Alt klasör `CLAUDE.md`'leri otomatik on-demand yüklenir (nested AGENTS.md'yi Claude desteklemez → servis içi 1 satırlık shim). Skills on-demand yüklenir + `/isim` ile çağrılır. Root anayasa her oturumda context'te olacağı için **≤150 satır**.

**Tek-kaynak kuralı:** her kural TEK dosyada yaşar. `AGENTS.md` = anayasa (Codex native okur, Claude import eder). `.claude/rules/` = yalnız mekanik stil kuralları. Servis içi `AGENTS.md` = yalnız o servisin invariant'ları (Codex native; Claude için yanına 1 satırlık `CLAUDE.md` shim: `@AGENTS.md`). Aynı kural iki yerde yazılmaz — kopya = drift.

**Token bütçesi — RULE vs REFERENCE ayrımı:** her zaman yüklü context küçük tutulur: `AGENTS.md` ~1–2k token (**meta-kural: 150 satırı aşarsa refactor edilir**) + `CLAUDE.md` ~100–300 token. Başka HİÇBİR doküman otomatik import edilmez — `@docs/SYSTEM_ARCHITECTURE.md + @EVENT_CATALOG + @30 ADR` zinciri token mangalıdır, YASAK. Ayrım: **RULE** = sürekli bilinmesi gereken ("başka servisin DB'sine sorgu yasak", "Marten sadece Order'da") → anayasada yaşar. **REFERENCE** = başvurulan bilgi ("OrderSubmitted payload'ında hangi alanlar var", "Marten neden seçildi") → docs/'ta yaşar, yalnız ilgili task'ta okunur. Anayasa bu yüzden **yön tabelası** içerir: *mimariye dokunan iş → SYSTEM_ARCHITECTURE.md oku; sahiplik → DATA_OWNERSHIP.md; messaging → EVENT_CATALOG.md; mevcut kararlar → docs/adr/; aktif kapsam → docs/plans/active/; bir servise dokunmadan önce en yakın AGENTS.md'yi oku.* Path'li `.claude/rules/` ve skill'ler zaten on-demand — kullanılmadıkça 0 token. Net etki: Catalog'da `CreateProduct` yazarken context'te Kubernetes kararı, PayTR idempotency'si veya VPS RAM bütçesi YOKTUR.

### Katman 0 — Faz 0'da doğar (minimal çekirdek; hepsinin içeriği bugünden hazır — "speculative abstraction yoksa speculative documentation da yok")

- **`AGENTS.md` (anayasa + yön tabelası, ≤150 satır):** misyon (ürün + öğrenme projesi; doğruluk ve açıklanabilirlik > hız), **MUST/SHOULD/MAY dili** (agent hangi kuralın sert olduğunu bilir), anti-iskelet kuralları (§4), cross-service DB yasağı, **Protected Decisions** (servis sınırları, DB sahipliği, messaging topolojisi, auth modeli, event sourcing kapsamı, public API sözleşmeleri, lisans stratejisi, hedef framework, deploy topolojisi — sorun görürsen: DUR, problemi tarif et, alternatif öner, onay bekle), **Scope Discipline** (ilgisiz refactor/rename/reformat/dependency upgrade yasak; diff görevle dar hizalı), **Learning Mode** (yeni mekanizma: önce problem → eğitici yerde naive implementasyon → kırılışını göster → çözüm → kanıtlayan test → DEVLOG; mekanizma anlaşılmadan library call'a geçilmez), **Verification zinciri** (format → build → unit → integration → diff review → doc update → DEVLOG → commit; "Done" = zincirin tamamı; koşulmayan test raporlanır), **yön tabelası** (token bütçesi paragrafındaki liste) ve doküman güncelleme matrisi (mimari→ADR, yeni event→EVENT_CATALOG, prosedür→runbook, ilerleme→devlog, kapsam→ROADMAP). Push daima kullanıcı onaylı; oturum sonu push'u `/finish-session` hatırlatır (veri kaybı dersi).
- **`CLAUDE.md` (≤20 satır):** `@AGENTS.md` + Claude'a özel: plan mode tetikleyicileri (mimari / çok-servisli / DB şeması / messaging / güvenlik değişiklikleri), "implementasyondan önce ilgili ADR'ları bul", "trade-off açıklanmadan önerilen mimari uygulanmaz".
- **`ARCHITECTURE.md` (root, 20–50 satır):** ASCII servis haritası + sahiplik listesi — agent repoya girince 2 dakikada şekli görür. Derin anlatım `docs/SYSTEM_ARCHITECTURE.md`'de; **diyagram tek yerde** (root), diğeri link verir.
- **`docs/SYSTEM_ARCHITECTURE.md` + `docs/ROADMAP.md`:** bu planın sentezi (gerektiğinde okunur, import edilmez).
- **`docs/DATA_OWNERSHIP.md` (küçük tablo):** entity→sahip + sert kuralın referansı: *hiçbir servis başka servisin PostgreSQL'ine sorgu atamaz; bilgi yalnız gRPC sorgusu / event / command ile akar* — AI'ın en tehlikeli kısayolu (`_catalogDb.Products` Order içinden 💀) baştan kapanır.
- **`docs/adr/`** + şablon (Status / Context / Decision / Alternatives / Consequences / **Exit Strategy — zorunlu bölüm, deneysel teknoloji kullanıyoruz**) + ilk ADR partisi.
- **`docs/devlog/2026-09.md`** (ay başına dosya).
- **İlk 2 skill (on-demand, ilk günden kullanımda):** `/finish-session` (DoD zinciri + DEVLOG + commit + push onayı) ve `/create-adr` (şablonla ADR üretir).

### Katman 1 — tetikleyiciyle doğar (içerik yoksa dosya da yok)

| Tetik | Doğan dosyalar |
|---|---|
| **Faz 1** (ilk C# kodu + ilk feature) | `src/Services/Catalog/AGENTS.md` (+1 satırlık `CLAUDE.md` shim: `@AGENTS.md`), `.claude/rules/dotnet.md` + `testing.md` (paths'li: file-scoped namespace, `CancellationToken`, `.Result`/`.Wait()` yasak, EF üstüne generic repository yasak, Domain→Infrastructure referans yasak; Testcontainers şart, EF InMemory PG yerine geçemez, race lab'ları önce hatayı ispatlar), `docs/DOMAIN_GLOSSARY.md` (*Dealer ≠ Customer, Product.Price ≠ checkout fiyatı, AvailableStock ≠ PhysicalStock, OrderSubmitted ≠ OrderConfirmed, PaymentCompleted ≠ OrderConfirmed*), `docs/API_CONVENTIONS.md` (ilk endpoint'le: URL şeması, status code haritası, RFC 7807, tek pagination zarfı), `docs/plans/active/001-*.md` (ilk feature planı: Goal / Scope / **Out of Scope** / API sözleşmesi / testler / DoD; bitince `completed/`e), `docs/runbooks/local-development.md` |
| **Faz 2** (frontend + Identity) | `frontend/AGENTS.md`, `.claude/rules/frontend.md` (TS strict, `any` yasak, server state=TanStack Query, Zustand=yalnız UI state, token browser JS'ine inmez, string'ler next-intl'den), `.claude/rules/security.md` |
| **Faz 3** (RabbitMQ girer) | `docs/EVENT_CATALOG.md` (kayıt başına: Type Command/Event · Producer · Consumers · Payload · Delivery at-least-once · idempotency şartı), `.claude/rules/messaging.md`, `/review-distributed-system` skill'i (checklist: *race? duplicate delivery? lost message? tx boundary? retry storm? idempotency? timeout? partial failure?*) |
| **Faz 4+** (servisler doğarken) | Order/Inventory/Payment `AGENTS.md`'leri (içerik: owns / does-not-own, persistence haritası, invariant'lar — örn. Order: "checkout fiyatını Catalog yeniden hesaplar", "saga adımında senkron gRPC yasak", "yalnız Order event-sourced"), `.claude/rules/database.md`, `/implement-feature` skill'i (sürtünme şekillenince) |
| **Faz 7** (deploy) | `docs/runbooks/vps-deployment.md` |
| İçerik birikince | `docs/TESTING_STRATEGY.md`, `OBSERVABILITY.md`, `SECURITY.md` |

### Kod düzeni (anayasaya girecek)

**Feature-first, teknik-klasör yasak:** `Products/CreateProduct/` altında Request + UseCase/Handler + Validator birlikte; `Services/ Repositories/ Managers/ Helpers/ Dtos/ Interfaces/` dağıtımı YASAK. CQRS servislerde aynı: `Orders/PlaceOrder/` altında Command + Handler + Validator + Result — bir feature için dört klasör gezilmez.

---

## Faz 0 "bitti" tanımı (onay sonrası ilk adım)

1. `git init` + GitHub'da **public** `yavuzcgg/AkironCommerce` + ilk push aynı gün.
2. `docker compose -f deploy/compose/docker-compose.infra.yml up -d` → PG/Redis/RMQ/ES/Jaeger/Aspire-dashboard/Mailpit hepsi `healthy`.
3. `dotnet build` (boş .slnx + CPM + Build.props) CI'da yeşil, warnings-as-errors açık.
4. §11 Katman 0 mevcut: `AGENTS.md` (≤150 satır) + `CLAUDE.md` (`@AGENTS.md`) + `ARCHITECTURE.md` + `docs/SYSTEM_ARCHITECTURE.md` + `docs/ROADMAP.md` + `docs/DATA_OWNERSHIP.md` + ilk ADR partisi + `docs/devlog/2026-09.md` ilk kaydı + `/finish-session`, `/create-adr` skill'leri.
5. Faz 1 sonunda gerçek uçtan uca kanıt: Catalog endpoint'ine istek → Jaeger'da trace + Testcontainers testi CI'da yeşil.

**İlk oluşturulacak dosyalar (öncelik sırasıyla):** `AGENTS.md`, `CLAUDE.md` (`@AGENTS.md`), `ARCHITECTURE.md`, `docs/SYSTEM_ARCHITECTURE.md` (bu planın sentezi), `docs/ROADMAP.md`, `Directory.Packages.props` (lisans pinleri), `deploy/compose/docker-compose.infra.yml`.
