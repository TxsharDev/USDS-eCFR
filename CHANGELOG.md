# Changelog

All notable changes to the eCFR Regulatory Analyzer are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) with [Semantic Versioning](https://semver.org/).

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
