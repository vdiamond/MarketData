# MarketData

.NET market-data **ingestion, extract, and feature-transform** pipelines. It downloads data from external market APIs (currently [EODHD](https://eodhd.com/)), stores it in a local LiteDB document database, builds date-keyed tabular extracts as CSV, then applies technical-analysis feature transforms for research and prediction workflows.

This is a set of **console ETL applications**, not a long-running web service.

## What it does

1. Calls market-data REST APIs (EODHD).
2. Deserializes responses into shared domain models (`MarketApi`).
3. Upserts documents into **LiteDB** collections (exchange symbols, fundamentals, EOD prices, sentiment).
4. Builds **date-keyed wide tables** (via DynamicTable) and writes **CSV** extracts.
5. Runs **feature transforms** (`eodhd_transform`) on extract CSVs using DynamicTable + TALib.
6. Reports **grouped statistics** (`eodhd_statistics`) on the transform feature CSV.

Typical pipeline flow:

```
EODHD API → RestClient → EodNetApi.Client → MarketApi models → LiteDB → CSV extracts → eodhd_transform → feature CSV → eodhd_statistics
```

## Repository layout

```
MarketData/
├── RestClient/           # Provider-agnostic HTTP + WebSocket client (BCL only)
├── eodhd_download/       # EODHD console app, domain models, orchestration
│   ├── Program.cs        # Load / extract pipeline
│   ├── Client.cs         # EODHD API adapter
│   ├── Model.cs          # Core entities
│   ├── Fundamental*.cs   # Stock / ETF / index fundamentals DTOs
│   └── ExtractUtil.cs    # Date spine + fill-forward helpers
├── eodhd_transform/      # Feature-engineering console (extract CSV → TA features)
│   └── Program.cs        # Load CSV, compute indicators, write feature CSV
├── eodhd_statistics/     # Grouped statistics console (feature CSV → report)
│   └── Program.cs        # Load feature CSV, group rows, print statistics
├── scripts/              # Secret-loading templates (local file gitignored)
└── .grok/rules/          # Coding standards and architecture notes
```

### Projects

| Project | Role |
|---------|------|
| **RestClient** | Generic REST GET client, query parameters, optional JSON deserialize, WebSocket helper |
| **eodhd_download** | EODHD adapter, LiteDB load stages, CSV extract stages |
| **eodhd_transform** | File-to-file TA / feature transform on extract CSVs (no LiteDB or API) |
| **eodhd_statistics** | Grouped statistics on transform feature CSVs (no LiteDB, API, or TALib) |

### External dependencies

Project references point at sibling repos. Clone them next to this repository so references resolve:

```
source_code/
├── MarketData/            # this repo
├── DynamicTable/          # Table, TableCsv, TableExtension
└── TechnicalIndicators/   # TALib (used by eodhd_transform)
```

## Requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK (or the version matching the project TFMs)
- An [EODHD](https://eodhd.com/) API token
- Optional: DynamicTable sibling projects for a full build of extracts

## Configuration

Do **not** hard-code API keys in source. Secrets are loaded via a **local, gitignored** PowerShell script.

| Variable | Purpose | Default |
|----------|---------|---------|
| `EODHD_API_TOKEN` | EODHD API key (**required**) | — |
| `MARKETDATA_DB_PATH` | Path to the LiteDB database file | `D:\LiteDb\EodHd.db` |
| `MARKETDATA_TRANSFORM_CSV` | Transform feature CSV (transform output and `eodhd_statistics` input) | `d:\predict\index_extract_ta.csv` |
| `MARKETDATA_EXTRACT_CSV` | Date-keyed extract CSV (download write and transform input) | `D:\predict\index_extract.csv` |
| `MARKETDATA_SYMBOLS_CSV` | Exchange-symbol universe CSV | `D:\predict\exchange_symbols.csv` |
| `MARKETDATA_SPX_SYMBOLS_CSV` | Index membership CSV | `D:\predict\SPX500_symbols.csv` |

### Local secrets script (recommended)

1. Copy the tracked template to a local file (already gitignored):

```powershell
Copy-Item .\scripts\set-secrets.example.ps1 .\scripts\set-secrets.local.ps1
```

2. Edit `scripts\set-secrets.local.ps1` and set your real token (and optional DB path).

3. **Dot-source** the local script so variables apply to the current session (note the leading `.`):

```powershell
. .\scripts\set-secrets.local.ps1
```

4. Run the app from that same PowerShell window:

```powershell
dotnet run --project eodhd_download/eodhd_download.csproj
```

| File | In git? | Purpose |
|------|---------|---------|
| `scripts/set-secrets.example.ps1` | Yes | Template with placeholders only |
| `scripts/set-secrets.local.ps1` | **No** (gitignored) | Your real secrets for this machine |

`set-secrets.local.ps1` only affects the current PowerShell session. Open a new terminal → load secrets again.

### Manual (one-off) session values

```powershell
$env:EODHD_API_TOKEN = "your-token-here"
$env:MARKETDATA_DB_PATH = "C:\Data\EodHd.db"
```

## Build

From the repository root (with DynamicTable available as a sibling):

```bash
dotnet build eodhd_download/eodhd_download.slnx
dotnet build eodhd_transform/eodhd_transform.slnx
dotnet build eodhd_statistics/eodhd_statistics.slnx
```

Or build individual projects:

```bash
dotnet build RestClient/RestClient.csproj
dotnet build eodhd_download/eodhd_download.csproj
dotnet build eodhd_transform/eodhd_transform.csproj
dotnet build eodhd_statistics/eodhd_statistics.csproj
```

## Run

```powershell
# From repo root — load secrets, then run download / extract
. .\scripts\set-secrets.local.ps1
dotnet run --project eodhd_download/eodhd_download.csproj

# Feature transform (after extract CSV exists; paths currently hard-coded in Program)
dotnet run --project eodhd_transform/eodhd_transform.csproj
# or: dotnet run --project eodhd_transform/eodhd_transform.csproj -- "D:\predict\index_extract.csv" "D:\predict\index_extract_ta.csv"
# `dotnet test MarketData.Tests/MarketData.Tests.csproj` covers fill-forward, EodFormatter, and the QQQ output column contract.

# Grouped statistics (after transform CSV exists)
dotnet run --project eodhd_statistics/eodhd_statistics.csproj
# or: dotnet run --project eodhd_statistics/eodhd_statistics.csproj -- "D:\predict\index_extract_ta.csv"
```

Targeted price load/join (does not rewrite the full extract):

```powershell
dotnet run --project eodhd_download/eodhd_download.csproj -- --load-prices IRX.INDX,FVX.INDX,TYX.INDX,MOVE.INDX,HYG.US,LQD.US
dotnet run --project eodhd_download/eodhd_download.csproj -- --join-prices IRX.INDX,FVX.INDX,TYX.INDX,MOVE.INDX,HYG.US,LQD.US
dotnet run --project eodhd_download/eodhd_download.csproj -- --load-prices TICK.INDX,IWM.US,SMH.US
dotnet run --project eodhd_download/eodhd_download.csproj -- --join-prices TICK.INDX,IWM.US,SMH.US
dotnet run --project eodhd_download/eodhd_download.csproj -- --load-prices US2Y.INDX,XLF.US,XLE.US
dotnet run --project eodhd_download/eodhd_download.csproj -- --join-prices US2Y.INDX,XLF.US,XLE.US
```

`--join-vix-terms` remains a shorthand for joining VIX1Y.INDX and VIX6M.INDX. `--load-prices` continues past a per-symbol 404/empty (MOVE.INDX or TICK.INDX may be omitted). `.US` ETFs that 404 retry the catalog exchange key (HYG.NYSE ARCA, IWM.NYSE ARCA, SMH.NASDAQ, XLF.NYSE ARCA, XLE.NYSE ARCA); MOVE.NASDAQ and a NASDAQ TICK are never substituted.

QQQ-member earnings calendar (does not rewrite prices or rebuild TA):

```powershell
dotnet run --project eodhd_download/eodhd_download.csproj -- --write-earnings
```

Dumps `D:\predict\qqq_member_earnings.csv` and left-joins `evt_earn` / `evt_earn_bmo` / `evt_earn_amc` / `evt_earn_n` onto the extract. HTTP 403 means the plan does not include calendar; the run stops and does not scrape.


QQQ 1-hour bars (does not rewrite prices, does not rebuild TA, does not FillIn missing hours):

```powershell
dotnet run --project eodhd_download/eodhd_download.csproj -- --write-qqq-intraday
```

Dumps `D:\predict\qqq_intraday_1h.csv` and left-joins `qqq_h1` / `qqq_hlast` / `qqq_h1_vol` / `qqq_hlast_vol` onto the extract for regular hours 09:30-16:00 ET. HTTP 403 or history starting after 2022 stops before join.

Pipeline stages in `eodhd_download` `Program.Main` are controlled by boolean feature flags (lookup symbols, fundamentals, prices, sentiment, extracts). Toggle those flags for iterative downloads.

## Data model highlights

- Instruments are identified as `{Symbol}.{Exchange}` (e.g. `QQQ.US`, `GSPC.INDX`).
- Price and sentiment rows use composite IDs like `{ExchangeSymbolId}.{Date}`.
- Dates in series and extracts are typically `yyyy-MM-dd` strings.

### LiteDB collections

| Collection | Content |
|------------|---------|
| `exchange_symbols` | Exchange symbol universe |
| `fundamental_etfs` | ETF fundamentals |
| `fundamental_indices` | Index fundamentals |
| `fundamental_stocks` | Stock fundamentals |
| `historical_day_prices` | End-of-day OHLCV |
| `sentiments_data` | Sentiment series |

## Architecture principles

- **RestClient** stays provider-agnostic (no EODHD URLs or domain types).
- **EodNetApi.Client** owns vendor endpoints and mapping into `MarketApi`.
- **MarketApi** models are the shared contract for API, storage, and extract.
- Orchestration stays in the download console app; ingest uses transactional upserts.
- **eodhd_transform** consumes extract CSV only — no LiteDB, RestClient, or MarketApi dependency.
- **eodhd_statistics** consumes transform feature CSV only — no LiteDB, RestClient, MarketApi, or TALib dependency.

More detail: `.grok/rules/ARCHITECTURE.md`.

## License

Private / unlicensed unless otherwise specified.

