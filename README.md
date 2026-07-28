# MarketData

.NET market-data **ingestion and extract** pipeline. It downloads data from external market APIs (currently [EODHD](https://eodhd.com/)), stores it in a local LiteDB document database, and builds date-keyed tabular extracts as CSV for research and prediction workflows.

This is a **console ETL / download application**, not a long-running web service.

## What it does

1. Calls market-data REST APIs (EODHD).
2. Deserializes responses into shared domain models (`MarketApi`).
3. Upserts documents into **LiteDB** collections (exchange symbols, fundamentals, EOD prices, sentiment).
4. Builds **date-keyed wide tables** (via DynamicTable) and writes **CSV** extracts.

Typical pipeline flow:

```
EODHD API → RestClient → EodNetApi.Client → MarketApi models → LiteDB → CSV extracts
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
└── .grok/rules/          # Coding standards and architecture notes
```

### Projects

| Project | Role |
|---------|------|
| **RestClient** | Generic REST GET client, query parameters, optional JSON deserialize, WebSocket helper |
| **eodhd_download** | EODHD adapter, LiteDB load stages, CSV extract stages |

### External dependency

The solution references sibling **DynamicTable** projects (`Table`, `TableCsv`, `TableExtension`) for tabular extract/CSV. Clone those next to this repository so project references resolve:

```
source_code/
├── MarketData/          # this repo
└── DynamicTable/        # external sibling
```

## Requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK (or the version matching the project TFMs)
- An [EODHD](https://eodhd.com/) API token
- Optional: DynamicTable sibling projects for a full build of extracts

## Configuration

Do **not** hard-code API keys in source.

| Variable | Purpose | Default |
|----------|---------|---------|
| `EODHD_API_TOKEN` | EODHD API key (**required**) | — |
| `MARKETDATA_DB_PATH` | Path to the LiteDB database file | `D:\LiteDb\EodHd.db` |

PowerShell example:

```powershell
$env:EODHD_API_TOKEN = "your-token-here"
$env:MARKETDATA_DB_PATH = "C:\Data\EodHd.db"
```

## Build

From the repository root (with DynamicTable available as a sibling):

```bash
dotnet build eodhd_download/eodhd_download.slnx
```

Or build individual projects:

```bash
dotnet build RestClient/RestClient.csproj
dotnet build eodhd_download/eodhd_download.csproj
```

## Run

```bash
# Set token first (see Configuration)
dotnet run --project eodhd_download/eodhd_download.csproj
```

Pipeline stages in `Program.Main` are controlled by boolean feature flags (lookup symbols, fundamentals, prices, sentiment, extracts). Toggle those flags for iterative downloads.

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
- Orchestration stays in the console app; ingest uses transactional upserts.

More detail: `.grok/rules/ARCHITECTURE.md`.

## License

Private / unlicensed unless otherwise specified.
