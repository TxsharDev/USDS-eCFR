# Changelog

All notable changes to the eCFR Regulatory Analyzer are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) with [Semantic Versioning](https://semver.org/).

---

## [1.9.0] — 2026-03-07

**Commit**: `feat: regulatory burden index chart and checksums table`

### Added
- `BurdenChart` color-coded horizontal bar chart with custom tooltip showing score breakdown
- `ChecksumTable` with copy-to-clipboard per row for easy hash comparison
- Dashboard now shows all 4 required metrics: word count, changes over time, checksums, RBI
- Charts displayed side by side on desktop, stacked on mobile
- "Highest RBI" metric card replaces "API Status" card

### Technical Notes
- RBI bars colored by intensity: green (<500), yellow (<1000), orange (<2000), red (2000+)
- Custom Recharts tooltip shows words, complexity, and RBI score together
- Clipboard API for one-click checksum copying with visual feedback
- Scrollable table with sticky header for large agency lists

---

## [1.8.0] — 2026-03-07

**Commit**: `feat: dashboard metrics and word count visualization`

### Added
- `MetricCard` component for reusable stat display
- `StatusBadge` component for backend health indicator
- `WordCountChart` horizontal bar chart (Recharts) showing top 15 agencies
- Dashboard wired to live API data with parallel fetching
- 4 metric cards: agencies count, change records, top word count, API status

### Technical Notes
- `Promise.all` for parallel data loading on mount
- Recharts `ResponsiveContainer` for fluid chart sizing
- Word counts formatted as millions (e.g. "59.5M") for readability
- Short names used in chart labels to prevent overflow

---

## [1.7.0] — 2026-03-07

**Commit**: `feat: frontend scaffold with React, Tailwind, and routing`

### Added
- Vite + React 19 project in `frontend/`
- Tailwind CSS v4 with Vite plugin for zero-config styling
- React Router with two routes: `/` (dashboard) and `/agency/:slug` (detail)
- API client module wrapping all backend endpoints
- USDS-branded header with logo, dark theme
- Vite dev proxy to backend on port 5000
- Placeholder Dashboard with live health status indicator

### Technical Notes
- Tailwind v4 uses `@import "tailwindcss"` instead of directives
- Vite proxy avoids CORS issues during development
- Dark theme (slate-900 background) for professional look

---

## [1.6.0] — 2026-03-07

**Commit**: `feat: background sync service with auto-refresh`

### Added
- `BackgroundSyncService` extends `BackgroundService` for automatic data sync
- Runs full sync on startup (after 5s delay), repeats every 24 hours
- Heatmap cache cleared after each sync so fresh data is served
- Registered as hosted service via `AddHostedService`

### Technical Notes
- Uses `IServiceScopeFactory` to create scoped services (SyncService, DbContext) from singleton context
- CancellationToken respected for clean shutdown
- Errors caught and logged per cycle, won't crash the app

---

## [1.5.0] — 2026-03-07

**Commit**: `feat: analytics REST endpoints`

### Added
- `AgencyEndpoints` with MapGroup `/api/agencies`
  - `GET /` list all agencies, sortable by words/rbi/name with optional limit
  - `GET /{slug}` agency detail with monthly change breakdown
- `AnalyticsEndpoints` with MapGroup `/api/analytics`
  - `GET /word-counts` top N agencies by word count
  - `GET /burden-index` top N agencies by RBI score
  - `GET /heatmap` agency x month change grid (top 15, cached)
  - `GET /checksums` all agency MD5 checksums
- Server-side heatmap cache with 5-min TTL, cleared on sync

### Technical Notes
- Extension methods on RouteGroupBuilder keep Program.cs clean
- Heatmap query aggregates 155K+ change records, cache avoids repeated expensive queries
- String comparison on ChangeDate column works because dates are stored as YYYY-MM-DD
- Cache invalidation wired to sync trigger endpoint

---

## [1.4.0] — 2026-03-06

**Commit**: `feat: version history sync and RBI computation`

### Added
- `SyncVersionsAsync()` fetches version history per title, maps changes to agencies via CFR references
- `SyncContentAsync()` fetches chapter XML, parses with `XDocument`, computes word count, sentence length, MD5 checksum
- Regulatory Burden Index (RBI) formula: `(words/1000) * (avgSL/15) * log2(staleness+1) / 100`
- Full sync pipeline: agencies -> titles -> versions -> content
- Duplicate change detection using composite key (slug + date + title)

### Technical Notes
- Version-to-agency mapping built from stored CFR references JSON
- XML text extraction pulls all text nodes from XDocument for word/sentence counting
- Staleness calculated from `latest_amended_on` date across referenced titles
- Batch saves every 25 agencies and every title to keep memory usage reasonable
- Individual fetch failures logged and skipped, don't abort the whole sync

---

## [1.3.0] — 2026-03-06

**Commit**: `feat: sync agencies and titles from eCFR`

### Added
- `SyncService` for data sync orchestration with `RunMetadataSyncAsync()` pipeline
- `SyncAgenciesAsync()` — fetches agencies from eCFR, flattens parent/child hierarchy, upserts into DB
- `SyncTitlesAsync()` — fetches all 50 CFR titles, upserts metadata
- `POST /api/sync/trigger` endpoint for manual sync with status response
- SyncLog entry written on each run with status tracking

### Technical Notes
- Upsert pattern: load existing records into dictionary, update or insert per DTO
- Agency hierarchy flattened recursively — children stored as top-level with their own CFR references
- CFR references serialized as JSON array in `CfrTitlesJson` column
- Error resilience: sync phases wrapped in try/catch, partial progress saved to SyncLog

---

## [1.2.0] — 2026-03-06

**Commit**: `feat: eCFR API client service`

### Added
- `EcfrClient` service for all eCFR API communication
- DTO records for API response deserialization (`AgenciesResponse`, `TitlesResponse`, `VersionsResponse`)
- Methods: `FetchAgenciesAsync()`, `FetchTitlesAsync()`, `FetchVersionsAsync()`, `FetchContentAsync()`
- Registered via `IHttpClientFactory` with 60-second timeout

### Technical Notes
- 100ms delay between sequential requests to avoid hammering eCFR public API
- Graceful error handling — returns null/empty on failure, caller decides recovery
- XML content fetched as raw `byte[]` for downstream parsing
- `System.Text.Json` with `JsonPropertyName` attributes for snake_case mapping

---

## [1.1.0] — 2026-03-06

**Commit**: `feat: health endpoint, EF Core database, and CORS`

### Added
- EF Core 9 with SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite 9.0.3`)
- 4 entity models: `Agency`, `Change`, `Title`, `SyncLog`
- `AppDbContext` with `OnModelCreating` — composite index on `Changes(AgencySlug, ChangeDate)`
- `GET /api/health` endpoint returning status, sync info, and record counts
- CORS middleware (`AllowAnyOrigin/Method/Header` for development)
- SQLite WAL mode enabled on startup for concurrent read performance
- Automatic database creation via `EnsureCreated()`

### Technical Notes
- DbContext registered as scoped service (one per request)
- No lazy loading explicit queries only
- Health endpoint uses async EF Core queries

---

## [1.0.0] — 2026-03-06

**Commit**: `chore: initial project structure and gitignore`

### Added
- ASP.NET Core 9 Minimal API project scaffold (`backend/EcfrAnalyzer.csproj`)
- Solution file (`EcfrAnalyzer.sln`)
- `.gitignore` configured for .NET, Node, SQLite, IDE, and Office temp files
- Minimal `Program.cs` with root health-check endpoint
- `appsettings.json` with fixed port binding (`http://localhost:5000`)
- This changelog

### Technical Notes
- Project targets `net9.0` with nullable reference types and implicit usings enabled
- No external dependencies at this stage, baseline scaffold only
