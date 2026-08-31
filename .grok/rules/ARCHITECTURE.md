# Architecture

**Purpose**  
This document describes the architecture of the MarketData solution: project layout, layering, data flow, patterns in use, and principles that **must** be followed when maintaining or extending the system.

**Scope**  
Applies to all new work and refactors. Prefer bringing existing code into alignment when you touch it.

**For AI Agents**  
Read this file together with `AGENTS.md` and `coding-standards.md` before changing structure, introducing dependencies, or adding features. Prefer extending existing patterns over inventing new ones.

---

## 1. Solution Overview

MarketData is a **.NET market-data ingestion, extract, and feature-transform** system. It:

1. Calls external market data APIs (currently **EODHD**) via reusable **EodHdExtract**.
2. Deserializes responses into **extract models** (`EodHdExtract` types).
3. Persists them in a local **LiteDB** document store (`eodhd_download` only).
4. Builds **date-keyed tabular extracts** (via DynamicTable) and writes **CSV** outputs.
5. Applies **technical-analysis feature transforms** on extract CSVs for prediction / research.
6. Computes **grouped descriptive statistics** on the transform feature CSV (`eodhd_statistics`).

It is primarily a set of **console ETL pipelines**, not a long-running web service. Ingest/extract, feature transform, and statistics are separate apps that share CSV as the hand-off contract.

| Concern | Technology |
|---------|------------|
| Language / runtime | C#; all solution projects target **net10.0** |
| HTTP / WebSocket | Custom `RestClient` library (BCL `HttpClient` / `ClientWebSocket`) |
| EODHD extract | **EodHdExtract** class library (`Client` + JSON models; no LiteDB) |
| Local persistence | **LiteDB** (ingest / `eodhd_download` only) |
| Tabular extract / CSV | External **DynamicTable** projects (`Table`, `TableCsv`, `TableExtension`) |
| Technical indicators | External **TechnicalIndicators** / `TALib` (transform only) |
| Statistics | Console report over transform CSV (`eodhd_statistics`; DynamicTable only) |
| Nullable | Enabled |

**Data directories**
All extracted and maintained data lives under `D:\grokbot`. The LiteDB database file is `D:\grokbot\LiteDb\EodHd.db`. Default CSV extract and transform paths are under `D:\grokbot`. Do not use `D:\Predict` or `D:\LiteDb`.

---

## 2. Repository Layout

```
MarketData/
├── .grok/rules/                 # Agent & project standards (this file, AGENTS, coding standards)
├── scripts/                     # Local secret-loading templates (example only; local file gitignored)
├── RestClient/                  # Provider-agnostic HTTP + WebSocket client library
│   ├── RestClientApi.cs
│   └── RestClientSocket.cs
├── EodHdExtract/                # Reusable EODHD extract library (no LiteDB)
│   ├── EodHdExtract.csproj
│   ├── Client.cs                # EODHD API adapter
│   ├── Model.cs                 # Core extract entities (exchange, symbol, prices, sentiment, calendars)
│   ├── FundamentalStock.cs      # Stock fundamentals DTOs
│   ├── FundamentalEtf.cs        # ETF fundamentals DTOs
│   └── FundamentalIndex.cs      # Index fundamentals DTOs
├── eodhd_download/              # EODHD download console: orchestration, LiteDB, CSV extract
│   ├── eodhd_download.slnx      # Ingest/extract solution
│   ├── Program.cs               # Pipeline orchestration (load / extract / export)
│   ├── ExtractUtil.cs           # Date-range table helpers, fill-forward
│   ├── ExtractJoin.cs           # Join stored prices onto an existing extract CSV
│   ├── EventCalendar.cs         # Economic-event CSV flags (uses EodHdExtract.Client)
│   ├── EarningsCalendar.cs      # Earnings-calendar CSV flags
│   ├── MacroIndicators.cs       # USA macro dump/join
│   └── QqqIntraday.cs           # QQQ 1h first/last hour join
├── MarketData.Tests/            # Regression tests (download helpers + EodHdExtract contracts)
├── eodhd_transform/             # Feature-engineering console app (CSV → TA features → CSV)
│   ├── eodhd_transform.slnx     # Transform solution
│   ├── eodhd_transform.csproj
│   └── Program.cs               # Load extract CSV, compute indicators, write feature CSV
└── eodhd_statistics/            # Grouped statistics console app (feature CSV → report)
    ├── eodhd_statistics.slnx
    ├── eodhd_statistics.csproj
    └── Program.cs               # Load feature CSV, group rows, print statistics
```

**External project references** (sibling repos, not under this workspace root):

| Sibling | Used by | Projects |
|---------|---------|----------|
| `DynamicTable` | `eodhd_download`, `eodhd_transform`, `eodhd_statistics` | `Table`, `TableCsv`, `TableExtension` |
| `TechnicalIndicators` | `eodhd_transform` only | `TALib` |

**Solution files** (separate apps; not a single monorepo solution):

| Solution | Projects |
|----------|----------|
| `eodhd_download/eodhd_download.slnx` | RestClient, EodHdExtract, eodhd_download, MarketData.Tests, DynamicTable |
| `eodhd_transform/eodhd_transform.slnx` | eodhd_transform, DynamicTable, TALib |
| `eodhd_statistics/eodhd_statistics.slnx` | eodhd_statistics, DynamicTable |

Expected sibling layout:

```
source_code/
├── MarketData/            # this repo
├── DynamicTable/          # tabular extract / CSV
└── TechnicalIndicators/   # TALib technical indicators
```

---

## 3. Logical Layers & Namespaces

Keep these boundaries clear. Dependencies should point **inward** toward extract models and **outward** only for I/O. Transform is a **downstream consumer of extract CSV**; it does not depend on LiteDB, RestClient, or EodHdExtract. Statistics is a **downstream consumer of transform CSV**; it does not depend on LiteDB, RestClient, EodHdExtract, or TALib.

```
┌─────────────────────────────────────────────────────────────────┐
│  Orchestration  (eodhd_download / Program)                       │
│  Load stages, extract stages, feature flags, file paths          │
└────────────────────────────┬────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐
│  EodHdExtract   │  │  Persistence    │  │  Extract / Export   │
│  Client         │  │  LiteDB         │  │  DateKeyedTableBuilder│
│  (no LiteDB)    │  │  collections    │  │  ExtractUtil, CSV    │
└────────┬────────┘  └────────▲────────┘  └──────────▲──────────┘
         │                    │                      │
         │           ┌────────┴────────┐             │ CSV hand-off
         └──────────►│  EodHdExtract   │◄────────────┘      │
                     │  JSON models    │                     │
                     └────────▲────────┘                     ▼
                              │ {ClassName}Id     ┌─────────────────────────┐
         ┌────────────────────┴────────────────┐   │  Feature transform       │
         │  Infrastructure: RestClient          │   │  eodhd_transform         │
         │  RestClientApi, QueryParameters,     │   │  DynamicTable + TALib    │
         │  ApiResponse, RestClientSocket       │   │  feature CSV output      │
         └──────────────────────────────────────┘   └───────────┬─────────────┘
                                                                │ CSV hand-off
                                                                ▼
                                                    ┌─────────────────────────┐
                                                    │  Statistics             │
                                                    │  eodhd_statistics       │
                                                    │  DynamicTable only      │
                                                    └─────────────────────────┘
```

| Namespace / area | Responsibility | Depends on |
|------------------|----------------|------------|
| **`RestClient`** | Generic REST GET client, query-string building, optional JSON deserialize, WebSocket client | BCL only |
| **`EodHdExtract`** | EODHD-specific endpoints (`Client`), JSON models with `[JsonPropertyName]` and `{ClassName}Id` | `RestClient` only (no LiteDB) |
| **`eodhd_download`** | Pipeline orchestration, LiteDB persistence, date-keyed extract builders, CSV export | EodHdExtract, LiteDB, DynamicTable |
| **`eodhd_transform`** | Load extract CSV, compute technical indicators / normalized features, write feature CSV | DynamicTable, TALib only |
| **`eodhd_statistics`** | Load transform feature CSV, group rows, print descriptive statistics | DynamicTable only |

### Layer rules

1. **`RestClient` must stay provider-agnostic.** No EODHD URLs, tokens, or market domain types.
2. **`EodHdExtract.Client`** owns EODHD endpoint paths, query conventions, and mapping quirks. Token is supplied by the caller (env `EODHD_API_TOKEN`); never hard-code it.
3. **`EodHdExtract` models are the extract contract.** Every Client-return type has `[JsonPropertyName]` on JSON fields and a `{ClassName}Id` property that returns a unique key. **Do not use `[BsonId]`.** `EodHdExtract.csproj` must not reference LiteDB. Nested fundamentals DTOs keep JSON names; document-root / Client-return types carry `{ClassName}Id` so LiteDB can store them later by convention.
4. **Orchestration and persistence stay in `eodhd_download`.** Loaders, collection mapping, extract-to-CSV, event/earnings/macro/intraday CSV joins remain in the app. The app consumes EodHdExtract then upserts into LiteDB.
5. **Do not reverse dependencies** (RestClient must not reference LiteDB or EodHdExtract; EodHdExtract must not reference LiteDB or `eodhd_download`).
6. **`eodhd_transform` must not take a dependency on LiteDB, RestClient, or EodHdExtract.** It consumes extract CSV and external TA/table libraries only. Keep the CSV column contract between download extract and transform explicit and stable.
7. **`eodhd_statistics` must not take a dependency on LiteDB, RestClient, EodHdExtract, or TALib.** It consumes transform feature CSV and DynamicTable only. Keep the CSV column contract between transform and statistics explicit and stable.

---

## 4. End-to-End Data Flow

Typical pipeline stages (ingest controlled by boolean flags in `eodhd_download` `Main`; transform is a separate console run):

```
EODHD HTTPS API
      │
      ▼
RestClientApi (HTTP + query params + ApiResponse)
      │
      ▼
EodHdExtract.Client (typed methods, deserialize / reshape)
      │
      ▼
EodHdExtract entities (`{ClassName}Id` + `[JsonPropertyName]`)
      │
      ▼
LiteDB in eodhd_download (upsert in transactions; composite string IDs)
      │
      ├──► Lookup / composite resolution (ETF holdings, index components)
      │
      ▼
DynamicTable extract (date spine + left-join series per symbol)
      │
      ▼
Fill-forward missing doubles → extract CSV files
      │
      ▼
eodhd_transform (load CSV → TALib / series transforms → feature CSV)
      │
      ├──► Prediction / research consumers
      │
      ▼
eodhd_statistics (load feature CSV → grouped statistics → console report)
```

### Load stages (ingest) — `eodhd_download`

| Stage | Source API (conceptual) | LiteDB collection | Model |
|-------|-------------------------|-------------------|--------|
| Exchange symbols | exchange-symbol-list | `exchange_symbols` | `ExchangeSymbol` |
| ETF fundamentals | fundamentals | `fundamental_etfs` | `FundamentalEtf` |
| Index fundamentals | fundamentals | `fundamental_indices` | `FundamentalIndex` |
| Stock fundamentals | fundamentals | `fundamental_stocks` | `FundamentalStock` |
| EOD historical prices | eod | `historical_day_prices` | `HistoricalStockPrice` |
| Sentiment | sentiments | `sentiments_data` | `SentimentsData` |

### Extract stages (export) — `eodhd_download`

| Output (example paths in current app) | Content |
|---------------------------------------|---------|
| Wide date-keyed price + sentiment CSV | Multi-symbol OHLCV / volume / sentiment on a weekday date spine |
| Exchange symbol universe CSV | All stored `ExchangeSymbol` rows |
| Index membership CSV | Components of a given index (e.g. GSPC) |

Paths and feature flags in `Main` are **environment-specific**; treat them as configuration that should eventually be externalized rather than hard-coded forever.

### Transform stages (feature engineering) — `eodhd_transform`

`eodhd_transform` is a **file-to-file** console app (namespace `TaCli`). It does not talk to EODHD or LiteDB.

| Step | Behavior (current Program) |
|------|----------------------------|
| Input | Wide extract CSV (e.g. `index_extract.csv`) loaded via DynamicTable with `Date` as string and other fields as double |
| Source series | Prefixed OHLCV / close / sentiment columns (e.g. `gspc.*`, `qqq.*`, `vix.Close`, `tnx.Close`, …) |
| Feature compute | Multi-horizon momentum (`mom`), normalize + EMA + sigmoid centering, MACD family, Wilder ATR momentum, annualized volatility windows, series offsets |
| Warm-up trim | Drop initial rows that lack full indicator history (currently first 260 rows) |
| Column selection | `TrimToInclude` keeps a curated feature set (today: `Date`, OHLCV-style targets, volatility/interest/dividend; feature columns evolve with the script) |
| Output | Feature CSV (e.g. `index_extract_ta.csv`) for downstream models |

**Contract note:** Transform depends on **column names and shapes** produced by the download extract. Renaming extract prefixes or dropping series can break transform without a compile-time error—treat extract column changes as a cross-app contract change.

### Statistics stages — `eodhd_statistics`

`eodhd_statistics` is a **file-to-console** app (namespace `eodhd_statistics`). It does not talk to EODHD, LiteDB, or TALib.

| Step | Behavior |
|------|----------|
| Input | Transform feature CSV (e.g. `index_extract_ta.csv`) loaded via DynamicTable with `Date` as `yyyy-MM-dd` and other fields as double. Path from argv[0], else `MARKETDATA_TRANSFORM_CSV`, else `D:\grokbot\index_extract_ta.csv`. |
| Groups | Full data set (record count, date from, date to); each calendar year; each month of year (all years combined); each weekday (all years/months combined); each day of month (all years combined). Empty buckets are omitted. |
| Statistics | Named, composable measures over row indexes. Current set: `% of records where qqq_yield_la1 > 0.0`; `% of records where qqq_yield_la1 > 0.0 for records where qqq_close > qqq_ema20`. Add new measures in `Program.Main` via `Statistic` factory methods. |
| Output | Console report (section per grouping). |

**Contract note:** Statistics depends on **column names** produced by transform (currently `Date`, `qqq_yield_la1`, `qqq_close`, `qqq_ema20`). Renaming or dropping those columns is a cross-app contract change.

---

## 5. Key Domain Concepts

### Exchange-symbol identity

Instruments are identified as **`{Symbol}.{Exchange}`** (e.g. `QQQ.US`, `GSPC.INDX`).

- Instrument identity stays `{Symbol}.{Exchange}`.
- LiteDB document keys follow LiteDB's own convention: a property named `Id`, or `{ClassName}Id` (for example `FundamentalStockId` on `FundamentalStock`). **Do not use `[BsonId]`.**
- Current document ids: `ExchangeId`, `ExchangeSymbolId`, `FundamentalStockId`, `FundamentalEtfId`, `FundamentalIndexId`, `HistoricalStockPriceId`, `SentimentsDataId`. Price and sentiment ids remain `{ExchangeSymbolId}.{Date}`.
- Additional Client-return keys (not necessarily persisted today): `MacroIndicatorId`, `EconomicEventId`, `EarningsCalendarResponseId`, `EarningsCalendarItemId`, `IntradayBarId`.
- That naming is what lets **EodHdExtract** stay free of the LiteDB library while `eodhd_download` still stores the same types.
- When parsing symbols for column prefixes, expect exactly two segments; invalid formats should fail fast.

### Dual-purpose models

`EodHdExtract` types:

- Deserialize from EODHD JSON via `[JsonPropertyName(...)]`.
- Expose a `{ClassName}Id` unique key so LiteDB can persist them by convention without a LiteDB attribute.
- Persist to LiteDB **only from `eodhd_download`** via collection mapping. Never use `[BsonId]`.
- Convert to tables via DynamicTable extensions (`ToTable()`) in the download app.

When adding a field, consider **all three** consumers (API JSON shape, storage identity/indexes, extract columns).

### Date handling

- API date parameters use `DateOnly` formatted as `yyyy-MM-dd` (via provider `IFormatter`).
- Stored series dates are typically **strings** in `yyyy-MM-dd` form for LiteDB queries and joins.
- Extracts build a **date spine** (trading weekdays by default) and left-join series onto it.

---

## 6. Patterns In Use

### 6.1 Provider adapter (facade)

`EodHdExtract.Client` wraps `RestClientApi`:

- Injects default query params (`api_token`, `fmt=json`).
- Exposes intent-named methods (`GetFundamentalsStockAsync`, `GetEndOfDayHistoricalPriceAsync`, …).
- Checks `ApiResponse.Ok`, throws meaningful errors, deserializes / reshapes into `EodHdExtract` types.

**When extending:** add new public methods on `EodHdExtract.Client` for new endpoints; do not call EODHD URLs from orchestration.

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
3. Stamp type-specific identity fields if needed (`FundamentalStockId`, `FundamentalEtfId`, `FundamentalIndexId`).
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

`eodhd_download` `Main` uses boolean switches to enable/disable stages (lookup, fundamentals, prices, sentiment, extracts). Useful for iterative runs; when hardening the app, replace with CLI args or config without scattering stage logic.

### 6.8 CSV feature transform (`eodhd_transform`)

`eodhd_transform` applies a script-style transform pipeline:

1. Load extract CSV into `IDynamicTable` with typed field attributes.
2. Bind source series via column names (`AsList<double>(...)`).
3. Allocate target feature columns (`NewColumAsList<double>`).
4. Chain TALib / series operations (momentum, normalize, EMA, MACD, ATR, volatility, offset).
5. Trim warm-up rows and optional column set; write feature CSV.

**When extending:** keep transforms pure functions of input series where possible; prefer named, stable output columns; externalize input/output paths rather than hard-coding machine paths; do not pull LiteDB or API clients into this project.

### 6.9 Grouped feature statistics (`eodhd_statistics`)

`eodhd_statistics` applies a small reporting pipeline:

1. Load transform CSV into `FeatureTable` (`Date` plus numeric columns).
2. Register named `Statistic` instances (start with percent-where; add more as research needs grow).
3. Build standard `RecordGroup`s (full set, year, month, weekday, day of month).
4. Print `StatisticReport` to the console.

**When extending:** add a new `Statistic` factory and register it in `Main`; keep grouping independent of any one measure; do not pull LiteDB, API clients, or TALib into this project.

---

## 7. Architectural Principles

These principles govern design decisions. When they conflict with short-term convenience, prefer the principle unless the user explicitly overrides.

### 7.1 Separation of concerns

| Do | Don't |
|----|--------|
| Keep HTTP plumbing in `RestClient` | Put EODHD paths or LiteDB into `RestClient` |
| Keep vendor mapping in `EodHdExtract.Client` | Scatter URL fragments across `Program` |
| Keep extract models in `EodHdExtract` with no LiteDB reference | Put `[BsonId]` or a LiteDB package on extract types |
| Keep orchestration and LiteDB in the app | Hide multi-stage ETL policy inside EodHdExtract |
| Keep feature engineering in `eodhd_transform` | Re-download or re-query LiteDB inside transform |
| Keep grouped statistics in `eodhd_statistics` | Recompute TA features or re-query LiteDB inside statistics |
| Pass data download → extract CSV → transform CSV → statistics | Couple transform or statistics to EodHdExtract or RestClient |

### 7.2 Single shared domain contract

- Prefer one set of models (`EodHdExtract`) for API ↔ store ↔ extract.
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
- Stick to BCL, existing Microsoft.Extensions usage (if introduced later), LiteDB (already used in download), established DynamicTable references, and TechnicalIndicators/TALib for transform.
- `RestClient` should remain dependency-free beyond the BCL.
- `EodHdExtract` may reference `RestClient` only. No LiteDB package, no `using LiteDB`.
- `eodhd_transform` should stay free of LiteDB, network clients, and EodHdExtract.
- `eodhd_statistics` should stay free of LiteDB, network clients, EodHdExtract, and TALib.

### 7.8 Hybrid vertical slice / clean architecture

For larger features:

- **Vertical slice:** group a feature end-to-end (provider method + model + load stage + optional extract) rather than spreading incomplete pieces.
- **Clean dependency rule:** outer layers (orchestration, infrastructure) depend on models and abstractions; models do not depend on orchestration.
- Interfaces are used where they already pay off (`IFormatter`, extract table interfaces from DynamicTable). Introduce new interfaces when you need substitution or test seams—not by default for every class.

### 7.9 Configuration and secrets

- **Do not commit real API tokens or machine-specific absolute paths as the long-term design.** Current hard-coded values in download/transform `Main` are a known smell; new work should move toward environment variables, user secrets, or config files outside source control.
- Database path, extract/transform I/O paths, and date ranges should be configurable when you next touch that area.
- Local secret loading for download uses `scripts/set-secrets.example.ps1` → gitignored `set-secrets.local.ps1` (`EODHD_API_TOKEN`, optional `MARKETDATA_DB_PATH`).
- `eodhd_statistics` input path: CLI arg, else `MARKETDATA_TRANSFORM_CSV`, else the current transform output default.

### 7.10 Observability

- Stage methods already time API and commit phases; keep structured timing/counts on new stages.
- Prefer `ILogger<T>` for any future hosted or multi-user tooling; console logging is acceptable for the current console ETL.

### 7.11 Extensibility playbook

| Goal | Approach |
|------|----------|
| New EODHD endpoint | Add method on `EodHdExtract.Client`; add/extend extract models with `{ClassName}Id` + `[JsonPropertyName]`; add load stage in `eodhd_download`; optionally extract |
| New data provider | New adapter project/namespace (mirror `EodHdExtract`); reuse `RestClient` and extract models where shapes align |
| New extract shape | Prefer `DateKeyedTableBuilder` / DynamicTable; avoid ad-hoc CSV string building; update transform if column contract changes |
| New TA feature / label | Extend `eodhd_transform` series bindings and TALib chains; keep warm-up trim consistent with longest lookback; update statistics if it reads the new column |
| New grouped statistic | Add a `Statistic` factory and register it in `eodhd_statistics` `Main`; reuse existing record groups |
| Streaming data | Use / extend `RestClientSocket` + provider adapter; persist via same collections/models when possible |
| Tests | Prefer testing pure extract helpers, mapping logic, and pure series transforms; mock HTTP at `RestClient` or adapter boundary without new frameworks unless approved |

---

## 8. LiteDB Collection Catalog

| Collection | Document type | Typical ID |
|------------|---------------|------------|
| `exchange_symbols` | `ExchangeSymbol` | `ExchangeSymbolId` (`{Symbol}.{Exchange}`) |
| `fundamental_etfs` | `FundamentalEtf` | `FundamentalEtfId` |
| `fundamental_indices` | `FundamentalIndex` | `FundamentalIndexId` |
| `fundamental_stocks` | `FundamentalStock` | `FundamentalStockId` |
| `historical_day_prices` | `HistoricalStockPrice` | `HistoricalStockPriceId` (`{ExchangeSymbolId}.{Date}`) |
| `sentiments_data` | `SentimentsData` | `SentimentsDataId` (`{ExchangeSymbolId}.{Date}`) |

When adding a collection:

1. Define the model in `EodHdExtract` with a stable `Id` or `{ClassName}Id` property and `[JsonPropertyName]` on JSON fields. Do not use `[BsonId]`.
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

1. **Orchestration is concentrated in `Program.cs`** — large surface in both `eodhd_download` and `eodhd_transform`; extract new types when a stage grows (loaders, extract writers, feature builders) without changing layer rules. `eodhd_statistics` already factors load / grouping / statistics / report types.
2. **Target framework** — all projects (including `RestClient`, DynamicTable, and TALib references) target **net10.0**.
3. **Secrets and paths** — download reads `EODHD_API_TOKEN` from the environment (do not hard-code it). Default data paths are `D:\grokbot\...`; override with env vars. Prefer env vars / scripts for secrets.
4. **Blocking waits** — some download pipeline stages use `.Wait()`; prefer full async main path when refactoring.
5. **External siblings** — DynamicTable and TechnicalIndicators live outside this repo; coordinate contract changes with those repositories.
6. **Three solution files** — download, transform, and statistics are separate apps/solutions sharing CSV convention, not project references between them.
7. **Transform is script-like** — much of `eodhd_transform` is explicit column wiring in `Main`; acceptable for research iteration, but factor helpers if feature sets grow.
8. **Tests** — `MarketData.Tests` covers download helpers, transform contracts, and EodHdExtract identity/`{ClassName}Id` plus the no-LiteDB assembly check. Keep tests free of live API keys.
9. **Extract vs LiteDB** — EODHD fetch/deserialize lives in **EodHdExtract** (no LiteDB). `eodhd_download` consumes that library and stores documents in LiteDB using `{ClassName}Id`. Nested fundamentals DTOs are JSON shapes, not separate LiteDB documents.

---

## 11. Decision Checklist (Before Merging Changes)

Use this list when reviewing your own or others' changes:

- [ ] Does the change respect layer boundaries (RestClient / EodHdExtract / download orchestration / transform / statistics)?
- [ ] Are new extract models dual-purpose ready (JSON names, `Id`/`{ClassName}Id` key, extract impact)? No `[BsonId]`. No LiteDB in EodHdExtract.
- [ ] Are identities and date formats consistent with existing composite keys?
- [ ] Are LiteDB writes transactional and idempotent (upsert)?
- [ ] Are async APIs cancellable?
- [ ] Were no unauthorized NuGet packages introduced?
- [ ] Would a new engineer find the feature by following an existing stage pattern?
- [ ] Were secrets or machine-specific paths avoided (or clearly temporary and not expanded)?
- [ ] If extract CSV columns changed, was `eodhd_transform` (or its contract docs) updated?
- [ ] If transform feature CSV columns changed, was `eodhd_statistics` (or its contract docs) updated?
- [ ] Does transform remain free of LiteDB / RestClient / EodHdExtract dependencies?
- [ ] Does statistics remain free of LiteDB / RestClient / EodHdExtract / TALib dependencies?

---

**Last Updated:** August 31, 2026  
**Version:** 1.5
)
