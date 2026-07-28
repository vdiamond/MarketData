# Architecture

**Purpose**  
This document describes the architecture of the MarketData solution: project layout, layering, data flow, patterns in use, and principles that **must** be followed when maintaining or extending the system.

**Scope**  
Applies to all new work and refactors. Prefer bringing existing code into alignment when you touch it.

**For AI Agents**  
Read this file together with `AGENTS.md` and `coding-standards.md` before changing structure, introducing dependencies, or adding features. Prefer extending existing patterns over inventing new ones.

---

## 1. Solution Overview

MarketData is a **.NET market-data ingestion and extract** system. It:

1. Calls external market data APIs (currently **EODHD**).
2. Deserializes responses into **domain models**.
3. Persists them in a local **LiteDB** document store.
4. Builds **date-keyed tabular extracts** (via DynamicTable) and writes **CSV** outputs for downstream use (e.g. prediction / research).

It is primarily a **console ETL / download pipeline**, not a long-running web service.

| Concern | Technology |
|---------|------------|
| Language / runtime | C#; all solution projects target **net10.0** |
| HTTP / WebSocket | Custom `RestClient` library (BCL `HttpClient` / `ClientWebSocket`) |
| Local persistence | **LiteDB** |
| Tabular extract / CSV | External **DynamicTable** projects (`Table`, `TableCsv`, `TableExtension`) |
| Nullable | Enabled |

---

## 2. Repository Layout

```
MarketData/
├── .grok/rules/                 # Agent & project standards (this file, AGENTS, coding standards)
├── RestClient/                  # Provider-agnostic HTTP + WebSocket client library
│   ├── RestClientApi.cs
│   └── RestClientSocket.cs
└── eodhd_download/              # EODHD download console app + domain models + orchestration
    ├── eodhd_download.slnx      # Solution file
    ├── Program.cs               # Pipeline orchestration (load / extract / export)
    ├── Client.cs                # EODHD API adapter (namespace EodNetApi)
    ├── Model.cs                 # Core MarketApi entities (exchange, symbol, prices, sentiment)
    ├── FundamentalStock.cs      # Stock fundamentals DTOs
    ├── FundamentalEtf.cs        # ETF fundamentals DTOs
    ├── FundamentalIndex.cs      # Index fundamentals DTOs
    └── ExtractUtil.cs           # Date-range table helpers, fill-forward
```

**External project references** (sibling repos, not under this workspace root):

- `DynamicTable/Table`
- `DynamicTable/TableCsv`
- `DynamicTable/TableExtension`

Solution file: `eodhd_download/eodhd_download.slnx` includes RestClient, eodhd_download, and the DynamicTable projects.

---

## 3. Logical Layers & Namespaces

Keep these boundaries clear. Dependencies should point **inward** toward domain models and **outward** only for I/O.

```
┌─────────────────────────────────────────────────────────────────┐
│  Orchestration  (eodhd_download / Program)                       │
│  Load stages, extract stages, feature flags, file paths          │
└────────────────────────────┬────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐
│  Provider API   │  │  Persistence    │  │  Extract / Export   │
│  EodNetApi      │  │  LiteDB         │  │  DateKeyedTableBuilder│
│  .Client        │  │  collections    │  │  ExtractUtil, CSV    │
└────────┬────────┘  └────────▲────────┘  └──────────▲──────────┘
         │                    │                      │
         │           ┌────────┴────────┐             │
         └──────────►│  MarketApi      │◄────────────┘
                     │  domain models  │
                     └────────▲────────┘
                              │ maps JSON / BSON
         ┌────────────────────┴────────────────────┐
         │  Infrastructure: RestClient              │
         │  RestClientApi, QueryParameters,         │
         │  ApiResponse, RestClientSocket           │
         └─────────────────────────────────────────┘
```

| Namespace / area | Responsibility | Depends on |
|------------------|----------------|------------|
| **`RestClient`** | Generic REST GET client, query-string building, optional JSON deserialize, WebSocket client | BCL only |
| **`EodNetApi`** | EODHD-specific endpoints, API key defaults, response shaping into `MarketApi` types | `RestClient`, `MarketApi` |
| **`MarketApi`** | Domain / DTO models shared by API, LiteDB, and extracts | LiteDB attributes, `System.Text.Json` |
| **`eodhd_download`** | Pipeline orchestration, load helpers, date-keyed extract builders, CSV export | All of the above + DynamicTable |

### Layer rules

1. **`RestClient` must stay provider-agnostic.** No EODHD URLs, tokens, or market domain types.
2. **Provider adapters** (e.g. `EodNetApi.Client`) own endpoint paths, query conventions, and mapping quirks for that vendor.
3. **`MarketApi` models are the shared contract** between download, storage, and extract. Prefer evolving models carefully; they are dual-purpose (JSON + LiteDB).
4. **Orchestration stays in the app project** (`Program` load/extract methods). Do not push pipeline policy into `RestClient`.
5. **Do not reverse dependencies** (e.g. RestClient must not reference LiteDB or MarketApi).

---

## 4. End-to-End Data Flow

Typical pipeline stages (controlled by boolean flags in `Main`):

```
EODHD HTTPS API
      │
      ▼
RestClientApi (HTTP + query params + ApiResponse)
      │
      ▼
EodNetApi.Client (typed methods, deserialize / reshape)
      │
      ▼
MarketApi entities
      │
      ▼
LiteDB (upsert in transactions; composite string IDs)
      │
      ├──► Lookup / composite resolution (ETF holdings, index components)
      │
      ▼
DynamicTable extract (date spine + left-join series per symbol)
      │
      ▼
Fill-forward missing doubles → CSV files
```

### Load stages (ingest)

| Stage | Source API (conceptual) | LiteDB collection | Model |
|-------|-------------------------|-------------------|--------|
| Exchange symbols | exchange-symbol-list | `exchange_symbols` | `ExchangeSymbol` |
| ETF fundamentals | fundamentals | `fundamental_etfs` | `FundamentalEtf` |
| Index fundamentals | fundamentals | `fundamental_indices` | `FundamentalIndex` |
| Stock fundamentals | fundamentals | `fundamental_stocks` | `FundamentalStock` |
| EOD historical prices | eod | `historical_day_prices` | `HistoricalStockPrice` |
| Sentiment | sentiments | `sentiments_data` | `SentimentsData` |

### Extract stages (export)

| Output (example paths in current app) | Content |
|---------------------------------------|---------|
| Wide date-keyed price + sentiment CSV | Multi-symbol OHLCV / volume / sentiment on a weekday date spine |
| Exchange symbol universe CSV | All stored `ExchangeSymbol` rows |
| Index membership CSV | Components of a given index (e.g. GSPC) |

Paths and feature flags in `Main` are **environment-specific**; treat them as configuration that should eventually be externalized rather than hard-coded forever.

---

## 5. Key Domain Concepts

### Exchange-symbol identity

Instruments are identified as **`{Symbol}.{Exchange}`** (e.g. `QQQ.US`, `GSPC.INDX`).

- Primary keys for prices, sentiment, and fundamentals use this composite form.
- LiteDB `[BsonId]` properties are often computed from component fields (e.g. `ExchangeSymbolIdDate => $"{ExchangeSymbolId}.{Date}"`).
- When parsing symbols for column prefixes, expect exactly two segments; invalid formats should fail fast.

### Dual-purpose models

`MarketApi` types:

- Deserialize from EODHD JSON via `[JsonPropertyName(...)]`.
- Persist to LiteDB via `[BsonId]` and collection mapping.
- Convert to tables via DynamicTable extensions (`ToTable()`).

When adding a field, consider **all three** consumers (API JSON shape, storage identity/indexes, extract columns).

### Date handling

- API date parameters use `DateOnly` formatted as `yyyy-MM-dd` (via provider `IFormatter`).
- Stored series dates are typically **strings** in `yyyy-MM-dd` form for LiteDB queries and joins.
- Extracts build a **date spine** (trading weekdays by default) and left-join series onto it.

---

## 6. Patterns In Use

### 6.1 Provider adapter (facade)

`EodNetApi.Client` wraps `RestClientApi`:

- Injects default query params (`api_token`, `fmt=json`).
- Exposes intent-named methods (`GetFundamentalsStockAsync`, `GetEndOfDayHistoricalPriceAsync`, …).
- Checks `ApiResponse.Ok`, throws meaningful errors, deserializes / reshapes into `MarketApi` types.

**When extending:** add new public methods on the provider client for new endpoints; do not call EODHD URLs from orchestration.

### 6.2 Generic REST client + result envelope

`RestClientApi`:

- Base address + relative paths.
- Parent/child `QueryParameters` (defaults vs per-call params; per-call cleared after use).
- `ApiResponse<T>` carries status, raw content, deserialized body, error message, exception.
- Optional deserialize flag (provider often deserializes raw string content itself for control).

**When extending:** keep error wrapping consistent; prefer extending `IFormatter` / `ParameterBlock` rather than string-concatenating query strings outside the client.

### 6.3 Strategy for query formatting

`IFormatter` + provider-specific implementation (`EodFormatter`) converts typed values (e.g. `DateOnly`) to API query strings.

### 6.4 ETL stage methods

Load helpers in `Program` follow a consistent shape:

1. Resolve LiteDB collection + ensure indexes.
2. Call provider client (async).
3. Stamp identity fields if needed (`ExchangeSymbolId`).
4. `BeginTrans` → `Upsert` → `Commit`; on failure `Rollback` and rethrow.
5. Log timing / counts to the console.

**When extending:** add a new stage method following the same transaction and identity patterns rather than inlining one-off DB writes in `Main`.

### 6.5 Date-keyed wide table builder

`DateKeyedTableBuilder`:

- Builds a base date-range table.
- Left-joins double series keyed by date string with a column prefix (typically lowercased ticker).
- Used to produce multi-asset extract matrices.

`ExtractUtil` fills missing numeric cells by **forward-filling** previous values.

### 6.6 WebSocket client (infrastructure)

`RestClientSocket` supports multi-resource WebSocket listen/send. Prefer reusing this infrastructure for streaming sources; keep provider protocol details in a dedicated adapter.

### 6.7 Feature-flagged pipeline

`Main` uses boolean switches to enable/disable stages (lookup, fundamentals, prices, sentiment, extracts). Useful for iterative runs; when hardening the app, replace with CLI args or config without scattering stage logic.

---

## 7. Architectural Principles

These principles govern design decisions. When they conflict with short-term convenience, prefer the principle unless the user explicitly overrides.

### 7.1 Separation of concerns

| Do | Don't |
|----|--------|
| Keep HTTP plumbing in `RestClient` | Put EODHD paths or LiteDB into `RestClient` |
| Keep vendor mapping in a provider client | Scatter URL fragments across `Program` |
| Keep models in `MarketApi` | Duplicate near-identical DTOs per layer without reason |
| Keep orchestration in the app | Hide multi-stage ETL policy inside library types |

### 7.2 Single shared domain contract

- Prefer one set of models (`MarketApi`) for API ↔ store ↔ extract.
- Identity rules (`ExchangeSymbolId`, date composite keys) must stay consistent across load and query.
- Document breaking changes to ID formats; they invalidate stored data.

### 7.3 Explicit I/O and async

- All network I/O is async; accept and propagate `CancellationToken` on new public API methods.
- Prefer `await` over `.Wait()` / `.Result` in new code (existing `Main` still uses blocking waits; migrate when touching it).
- Never swallow exceptions in load/extract stages; rollback transactions and rethrow.

### 7.4 Fail fast with clear errors

- Null deserialization → throw with type context.
- Missing prerequisite data (e.g. ETF not in DB before holdings resolve) → throw with entity id.
- Invalid symbol format → throw before writing partial extracts.

### 7.5 Idempotent ingest

- Prefer **upsert** by stable IDs so re-runs are safe.
- Ensure indexes used by queries/upserts (`Symbol`, `Exchange`, `ExchangeSymbolId`, etc.).
- Keep transactions tight around DB mutations for a logical batch.

### 7.6 Composition over inheritance

- Prefer small collaborating types (formatter, query params, table builder) over deep hierarchies.
- `sealed` by default for new types unless inheritance is required.

### 7.7 Minimal dependencies

- Do **not** add NuGet packages unless explicitly requested or already approved.
- Stick to BCL, existing Microsoft.Extensions usage (if introduced later), LiteDB (already used), and established DynamicTable references.
- `RestClient` should remain dependency-free beyond the BCL.

### 7.8 Hybrid vertical slice / clean architecture

For larger features:

- **Vertical slice:** group a feature end-to-end (provider method + model + load stage + optional extract) rather than spreading incomplete pieces.
- **Clean dependency rule:** outer layers (orchestration, infrastructure) depend on models and abstractions; models do not depend on orchestration.
- Interfaces are used where they already pay off (`IFormatter`, extract table interfaces from DynamicTable). Introduce new interfaces when you need substitution or test seams—not by default for every class.

### 7.9 Configuration and secrets

- **Do not commit real API tokens or machine-specific absolute paths as the long-term design.** Current hard-coded values in `Main` are a known smell; new work should move toward environment variables, user secrets, or config files outside source control.
- Database path, extract output paths, and date ranges should be configurable when you next touch that area.

### 7.10 Observability

- Stage methods already time API and commit phases; keep structured timing/counts on new stages.
- Prefer `ILogger<T>` for any future hosted or multi-user tooling; console logging is acceptable for the current console ETL.

### 7.11 Extensibility playbook

| Goal | Approach |
|------|----------|
| New EODHD endpoint | Add method on `EodNetApi.Client`; add/extend `MarketApi` models; add load stage; optionally extract |
| New data provider | New adapter project/namespace (mirror `EodNetApi`); reuse `RestClient` and `MarketApi` where shapes align |
| New extract shape | Prefer `DateKeyedTableBuilder` / DynamicTable; avoid ad-hoc CSV string building |
| Streaming data | Use / extend `RestClientSocket` + provider adapter; persist via same collections/models when possible |
| Tests | Prefer testing pure extract helpers and mapping logic; mock HTTP at `RestClient` or adapter boundary without new frameworks unless approved |

---

## 8. LiteDB Collection Catalog

| Collection | Document type | Typical ID |
|------------|---------------|------------|
| `exchange_symbols` | `ExchangeSymbol` | `{Symbol}.{Exchange}` |
| `fundamental_etfs` | `FundamentalEtf` | `ExchangeSymbolId` |
| `fundamental_indices` | `FundamentalIndex` | `ExchangeSymbolId` |
| `fundamental_stocks` | `FundamentalStock` | `ExchangeSymbolId` |
| `historical_day_prices` | `HistoricalStockPrice` | `{ExchangeSymbolId}.{Date}` |
| `sentiments_data` | `SentimentsData` | `{ExchangeSymbolId}.{Date}` |

When adding a collection:

1. Define the model in `MarketApi` with a stable `[BsonId]`.
2. Name the collection with clear, plural snake-ish names consistent with the table above.
3. Ensure indexes for fields used in filters and joins.
4. Document the collection in this catalog.

---

## 9. Coding Alignment

All code changes must also follow:

- `.grok/rules/coding-standards.md` — member ordering, braces, modern C#, async rules, no `goto`, etc.
- `.grok/rules/AGENTS.md` — workflow, testing, dependency policy.

Architecture decisions in this file take precedence for **structure and boundaries**; coding-standards take precedence for **style and language usage**.

---

## 10. Known Constraints & Evolution Notes

These are factual characteristics of the current codebase; use them to plan improvements carefully:

1. **Orchestration is concentrated in `Program.cs`** — large surface; extract new types when a stage grows (loaders, extract writers) without changing layer rules.
2. **Target framework** — all projects (including `RestClient` and DynamicTable libraries) target **net10.0**.
3. **Secrets and paths** — hard-coded API token and `D:\...` paths exist; treat removal as a priority when editing `Main`.
4. **Blocking waits** — some pipeline stages use `.Wait()`; prefer full async main path when refactoring.
5. **DynamicTable is external** — coordinate contract changes with the DynamicTable repositories.
6. **No automated test project yet** — when adding tests, place them in a dedicated test project and keep them free of live API keys.

---

## 11. Decision Checklist (Before Merging Changes)

Use this list when reviewing your own or others' changes:

- [ ] Does the change respect layer boundaries (RestClient / provider / MarketApi / orchestration)?
- [ ] Are new models dual-purpose ready (JSON names, BSON id, extract impact)?
- [ ] Are identities and date formats consistent with existing composite keys?
- [ ] Are LiteDB writes transactional and idempotent (upsert)?
- [ ] Are async APIs cancellable?
- [ ] Were no unauthorized NuGet packages introduced?
- [ ] Would a new engineer find the feature by following an existing stage pattern?
- [ ] Were secrets or machine-specific paths avoided (or clearly temporary and not expanded)?

---

**Last Updated:** July 2026  
**Version:** 1.0
)
