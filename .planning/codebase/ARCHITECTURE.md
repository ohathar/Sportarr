<!-- refreshed: 2026-06-01 -->
# Architecture

**Analysis Date:** 2026-06-01

## System Overview

Sportarr is a full-stack sports PVR application built on ASP.NET Core 8 with a React TypeScript frontend. The system monitors sports leagues, searches indexers for releases, manages downloads, imports media files, and integrates with media servers (Plex, Jellyfin, Emby) for library metadata.

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│                         Frontend Layer                                       │
│              React + TypeScript + Vite + React Query                         │
│     `frontend/src/` — Pages, Components, API hooks, Context providers        │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ HTTP/REST
┌──────────────────────────────────────▼──────────────────────────────────────┐
│                      API Layer (HTTP Endpoints)                              │
│          `src/Endpoints/*Endpoints.cs` — Grouped by domain                   │
│  /api/events  /api/leagues  /api/iptv  /api/download-clients  etc.          │
└──────────────┬──────────────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────────────┐
│                     Business Logic & Services                                │
│           `src/Services/*.cs` — 90+ domain-specific services                 │
│  IndexerSearchService, ReleaseEvaluator, FileImportService, etc.            │
└──────────────┬──────────────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────────────┐
│                    Data & Persistence Layer                                  │
│    `src/Data/SportarrDbContext.cs` — EF Core on SQLite                      │
│    Models: Event, League, Team, EventFile, DownloadQueue, etc.              │
└──────────────┬──────────────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────────────┐
│                    External Integrations                                     │
│  Sportarr API, Indexers (Newznab/Torznab), Download Clients,               │
│  IPTV Sources, Media Servers, FFmpeg, File System                            │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| **Endpoints** | HTTP request routing, validation binding, response formatting | `src/Endpoints/*Endpoints.cs` (45 files) |
| **IndexerSearchService** | Multi-indexer search orchestration, quality scoring, rate limiting | `src/Services/IndexerSearchService.cs` |
| **ReleaseEvaluator** | Release quality assessment, custom format matching, retention logic | `src/Services/ReleaseEvaluator.cs` |
| **EventQueryService** | Search query templating for different sports/leagues | `src/Services/EventQueryService.cs` |
| **LeagueEventSyncService** | Syncing events from Sportarr API to local database | `src/Services/LeagueEventSyncService.cs` |
| **FileImportService** | Processing imported event files from downloads | `src/Services/FileImportService.cs` |
| **FileRenameService** | Renaming files according to naming configuration | `src/Services/FileRenameService.cs` |
| **DvrRecordingService** | IPTV DVR scheduling and execution | `src/Services/DvrRecordingService.cs` |
| **DownloadClientService** | Download client adapter orchestration | `src/Services/DownloadClientService.cs` |
| **SportarrDbContext** | EF Core DbContext with all model definitions | `src/Data/SportarrDbContext.cs` |
| **Download Clients** | Adapters for qBittorrent, Deluge, SABnzbd, Transmission, etc. | `src/Services/*.Client.cs` |
| **Authentication** | API key, basic auth, form-based session management | `src/Authentication/` |
| **Middleware** | CORS, exception handling, request logging, version headers | `src/Middleware/` |
| **Validators** | FluentValidation request DTO validation | `src/Validators/*Validator.cs` |

## Pattern Overview

**Overall:** Layered architecture with horizontal domain separation. The system follows patterns from Sonarr (which Sportarr is based on) but tailored for sports events instead of TV shows.

**Key Characteristics:**
- **HTTP-first API**: All business logic accessible via RESTful endpoints grouped by domain
- **DI-driven services**: Dependency injection container manages all stateful services; no `new` keyword for application code
- **Background workers**: Multiple BackgroundService implementations poll for changes (downloads, events, RSS, DVR scheduling)
- **Stateless endpoints**: Endpoints inject services and invoke them; no request-scoped state
- **EF Core + SQLite**: Single SqlLite database for all domain entities, migrated via EF Core
- **Frontend-driven**: React UI consumes the native `/api/*` endpoints; Sonarr v1/v3 shims exist for ecosystem integration

## Layers

**Endpoint Layer:**
- Purpose: Handle HTTP requests, route to services, return JSON/status codes
- Location: `src/Endpoints/`
- Contains: Static extension methods that map routes, request validation, response formatting
- Depends on: Services, DbContext, Validators, Models
- Used by: Frontend (React), External tools (Prowlarr, Decypharr, Maintainerr)

**Service Layer:**
- Purpose: Business logic, orchestration, external API calls, data transformation
- Location: `src/Services/`
- Contains: ~90 domain-specific services split by concern (indexers, downloads, IPTV, file management, sync)
- Depends on: DbContext, other services, HttpClientFactory, ILogger
- Used by: Endpoints, other services (service-to-service injection), background workers

**Data Layer:**
- Purpose: Persistence abstraction via EF Core
- Location: `src/Data/SportarrDbContext.cs`, `src/Models/`
- Contains: DbContext with 50+ DbSet definitions, domain entities with navigation properties
- Depends on: Entity Framework Core, SQLite
- Used by: All services and endpoints

**Helper Layer:**
- Purpose: Pure static utility functions (no DI, no state)
- Location: `src/Helpers/`
- Contains: PartRelevanceHelper, HlsRewriter, TennisLeagueHelper, etc.
- Depends on: Only standard library or models
- Used by: Services, validators

## Data Flow

### Primary Request Path: Search → Grade → Grab → Download

1. User triggers manual search from UI → `EventSearchAndGrabEndpoints.cs:ManualSearchEvent` (endpoint)
2. Endpoint injects `IndexerSearchService` → calls `SearchAsync(eventId, userInitiated: true)` (service)
3. Service builds query via `EventQueryService.BuildEventQueries(event)` (service chain)
4. Service queries indexers in parallel (max 5 concurrent) via `IHttpClientFactory` (named client: `IndexerClient`)
5. Each indexer's releases parsed, stored in temporary `SearchResultCache`
6. Service invokes `ReleaseEvaluator.EvaluateRelease(...)` for each result → scores each by quality, custom formats
7. Top-scoring release auto-grabbed if meets threshold, or user manually picks one
8. Grab creates `DownloadQueueItem` in database, invokes `DownloadClientService.AddAsync(release)`
9. Download client adapter (`QBittorrentClient`, `SabnzbdClient`, etc.) sends to respective client
10. `EnhancedDownloadMonitorService` (background worker) polls download clients every 30s → updates `DownloadQueueItem.Status`
11. When download completes, `CompletedDownloadHandlingService` moves file to import folder
12. `FileImportService` (endpoint-triggered or auto-polled) processes file → matches to event → renames via `FileRenameService`
13. `FileWatcherService` detects renamed file in media library → triggers optional post-import (refresh, notifications)

### League Event Sync Flow

1. User opens league detail or background `LeagueEventAutoSyncService` triggers periodic sync
2. Endpoint → `LeagueEventSyncService.SyncLeagueEventsAsync(leagueId, seasons, ...)`
3. Service fetches seasons from `SportarrApiClient` (client for sportarr.net API)
4. Parses received events, upserts to `Events` table with date-based episode numbering
5. Detects season changes, flags affected events for file renaming
6. Returns sync result with counts (new, updated, skipped)
7. Frontend UI displays progress via TaskService callback mechanism

### DVR Recording Flow (IPTV/Alpha)

1. IPTV source added → `IptvSourceService` fetches M3U playlist, parses channels
2. Optional: EPG source added → `XmltvParserService` parses EPG guide
3. User maps channel to league: `ChannelLeagueMapping` created
4. User monitors an event → `DvrAutoSchedulerService` (background) checks if league has mapped channel
5. If matched: `DvrRecordingService` schedules a `DvrRecording` entry, sets status to Scheduled
6. `DvrSchedulerService` (background, runs every 5 minutes) finds due recordings, spawns `FFmpegRecorderService`
7. FFmpeg reads IPTV stream, records to `.ts` file with ring buffer (handles stream reconnection)
8. Recording completes → file moved to import folder → `FileImportService` auto-imports
9. `DvrWatchdogService` monitors long-running FFmpeg processes, kills stuck recordings

### State Management

- **Download state**: Tracked in `DownloadQueueItem`, synced by `EnhancedDownloadMonitorService` polling every 30s
- **Import queue**: `PendingImport` entities stage files before final import
- **Event state**: Event `Status` field (Upcoming, Airing, Completed) updated during sync and import
- **Task progress**: `AppTask` entity holds state, progress callbacks update it during long-running operations
- **Search state**: Static `ActiveSearchStatus` in `IndexerSearchService` drives UI bottom-left indicator

## Key Abstractions

**ReleaseEvaluator:**
- Purpose: Universal quality assessment logic for releases across all sports
- Examples: `src/Services/ReleaseEvaluator.cs`
- Pattern: Single service with multiple scoring methods (`EvaluateRelease`, `CalculateScore`, `CheckCustomFormats`)
- Used by: IndexerSearchService, ReleaseMatchingService

**EventQueryService:**
- Purpose: Build sport-specific search queries from events
- Examples: `src/Services/EventQueryService.cs`
- Pattern: Template-based query builder supporting tokens ({League}, {HomeTeam}, {AwayTeam}, {Round}, {Year}, etc.)
- Used by: IndexerSearchService, ManualSearchService

**Download Client Adapters:**
- Purpose: Abstract differences between qBittorrent, Deluge, SABnzbd, Transmission, rTorrent, NZBGet
- Examples: `src/Services/QBittorrentClient.cs`, `src/Services/SabnzbdClient.cs`, etc.
- Pattern: Base class `BaseDownloadClient`, each adapter implements `AddAsync`, `GetStatusAsync`, `RemoveAsync`
- Used by: DownloadClientService (factory pattern)

**IPTV Parsing Pipeline:**
- Purpose: Handle heterogeneous IPTV sources (M3U, Xtream Codes, EPG)
- Examples: `src/Services/M3uParserService.cs`, `src/Services/XmltvParserService.cs`, `src/Services/XtreamCodesClient.cs`
- Pattern: Each parser produces normalized `IptvChannel` and `EpgProgram` entities
- Used by: IptvSourceService, IptvOrgSyncService

**FileImportMatcher:**
- Purpose: Match downloaded/renamed files to events by parsing names and metadata
- Examples: `src/Services/FileImportService.cs`, `src/Services/SportsFileNameParser.cs`
- Pattern: Parse file path/name → extract league, date, teams → fuzzy match to events
- Used by: FileImportService, FileWatcherService

## Entry Points

**Program.cs:**
- Location: `src/Program.cs` (~960 lines)
- Triggers: Application startup
- Responsibilities: 
  - Parse command-line arguments (-data, --tray, --help)
  - Resolve data path (Windows ACL aware, environment-aware)
  - Configure Serilog logging with file sinks
  - Build dependency injection container (call `AddSportarr*` extension methods)
  - Configure middleware pipeline (CORS, exception handling, auth, request logging)
  - Register all endpoint groups (`MapXxxEndpoints()`)
  - Start background services
  - Windows-specific: tray integration

**HTTP Endpoints (45 files):**
- Pattern: Each endpoint file is a static class with one `MapXxx` extension method
- Examples: 
  - `EventSearchAndGrabEndpoints.cs` → `/api/events`, `/api/events/search`, `/api/events/grab`
  - `IptvEndpoints.cs` → `/api/iptv/sources`, `/api/iptv/channels`, `/api/iptv/filtered.m3u`
  - `DownloadClientEndpoints.cs` → `/api/download-clients`, `/api/download-clients/test`
- Registration: Wired once in `Program.cs` with one-liner

**Background Services (14 registered):**
- Location: `src/Startup/ServiceCollectionExtensions.cs` → `AddSportarrBackgroundServices()`
- Services:
  - `LeagueEventAutoSyncService` — periodically syncs monitored leagues
  - `RssSyncService` — polls indexer RSS feeds
  - `EnhancedDownloadMonitorService` — tracks downloads in-flight
  - `CompletedDownloadHandlingService` — moves completed downloads to import folder
  - `FileWatcherService` — monitors media library for new files
  - `DiskScanService` — scans root folders for untracked files
  - `BacklogSearchService` — searches for wanted/missing episodes
  - `DvrSchedulerService`, `DvrAutoSchedulerService`, `DvrWatchdogService` — IPTV recording management
  - `PendingReleaseReaperService` — garbage-collects stale pending releases
  - `TrashSyncBackgroundService`, `EventMappingSyncBackgroundService`, `TvScheduleSyncService`
- Pattern: Each implements `BackgroundService`, runs `ExecuteAsync(cancellationToken)` loop

## Architectural Constraints

- **Threading:** Single-threaded event loop at HTTP level. Background services use `Task.Delay` loops. Database context is thread-safe per async call. See `IDbContextFactory<>` usage in background services that spawn Task.WhenAll for concurrent database access.
- **Global state:** 
  - Static `ActiveSearchStatus` in `IndexerSearchService` (locked) — drives UI search indicator
  - Static `_currentSearch` in IndexerSearchService — same purpose
  - Database-backed task queue in `AppTask` table (persisted across restarts)
- **Circular imports:** None detected. Dependency graph is acyclic via DI (Program.cs orchestrates registration order).
- **SQLite Limitations:** Single writer per transaction; background services stagger database writes. File locking on network shares known limitation.
- **Frontend Build:** React frontend auto-built by `Sportarr.csproj` target `BuildFrontend` before .NET build. Output copied to `wwwroot/`. Can skip with `-p:SkipFrontendBuild=true`.
- **Configuration Storage:** All app settings in `AppSettings` table, synced via `ConfigService` at startup. Runtime config changes persisted to database.

## Anti-Patterns

### String-Based Comparisons in Download Clients

**What happens:** Download IDs compared case-sensitively across client list/poll responses, missing matches when clients return different cases.

**Why it's wrong:** qBittorrent and SABnzbd inconsistently case their torrent/NZB hashes. Case-sensitive `HashSet` lookups miss, causing grabbed downloads to re-appear as "external" imports.

**Do this instead:** Use `StringComparer.OrdinalIgnoreCase` for download ID HashSets. See `EnhancedDownloadMonitorService.DetectExternalDownloadsAsync()` — all three lookup sets now use case-insensitive comparer.

### Direct HTTP Client Instantiation

**What happens:** Creating `new HttpClient()` in services instead of using `IHttpClientFactory`.

**Why it's wrong:** Violates socket exhaustion protection. Each `new HttpClient()` holds a socket pool; creating many instances leaks sockets and breaks retry policies.

**Do this instead:** Inject `IHttpClientFactory`, call `CreateClient("NamedClientKey")`. Named clients configured once in `ServiceCollectionExtensions.AddSportarrHttpClients()` with retry policy, timeout, and connection pooling.

### Synchronous Database Reads in Endpoints

**What happens:** Using `DbContext.Find(id)` synchronously when async is available.

**Why it's wrong:** Blocks the threadpool; reduces throughput on high-concurrency endpoints.

**Do this instead:** Use `async/await` throughout. Endpoints are declared `async`, services use async database methods.

## Error Handling

**Strategy:** Three-tier approach — validation, try-catch, global middleware.

**Patterns:**
- **Request validation**: FluentValidation in `Validators/` applied via `.WithRequestValidation<T>()` on endpoints. Returns 400 BadRequest with field errors.
- **Service exceptions**: Services throw typed exceptions (`SportarrException` or subtypes). Caught in endpoints, returned as 400/500 with ProblemDetails JSON.
- **Unhandled exceptions**: Global `ExceptionHandlingMiddleware` in `Middleware/` catches all; logs with stack trace, returns 500 ProblemDetails. Never leaks internals to client.
- **Database failures**: EF Core throws `DbUpdateException` on constraint violations; caught by middleware or endpoints, mapped to validation-friendly response.

## Cross-Cutting Concerns

**Logging:** 
- Uses Serilog with file and console sinks
- Every class injects `ILogger<T>` (structured logging)
- Prefix pattern: `[DOMAIN]` in message (e.g., `[IPTV]`, `[SEARCH]`, `[IMPORT]`)
- Levels: Error (unrecoverable), Warning (retry succeeded), Information (state change), Debug (diagnostic), Trace (never)

**Validation:** 
- Request bodies: FluentValidation validators in `src/Validators/`
- Database entities: EF Core data annotations (`[Required]`, `[MaxLength]`)
- Complex rules: Custom validators inheriting `AbstractValidator<T>`

**Authentication:** 
- API key (header `X-Api-Key`) validated via `ApiKeyAuthHandler`
- Basic auth supported via `BasicAuthHandler`
- Session-based (forms auth) via `AuthSession` table, validated in middleware
- Dynamic auth via `UseDynamicAuthentication` middleware — allows login via any method

**Rate Limiting:**
- Per-indexer: `RateLimitHandler` wraps HttpClient, enforces 2-second delay + jitter
- Per-endpoint: `RateLimitService` in memory, tracks requests by client IP
- Exponential backoff for failed indexers: 1m → 5m → 15m → 30m → 1h → 24h

---

*Architecture analysis: 2026-06-01*
