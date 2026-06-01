# External Integrations

**Analysis Date:** 2026-06-01

## APIs & External Services

**Sports Metadata:**
- **Sportarr API (sportarr.net)** - Primary sports data provider (leagues, teams, players, events, TV schedules)
  - SDK/Client: `SportarrApiClient` (`src/Services/SportarrApiClient.cs`)
  - Configuration: `SportarrApi:BaseUrl` (defaults to `https://sportarr.net/api/v2/json`)
  - Custom endpoint: `CustomMetadataApiUrl` in `config.xml` for self-hosted instances
  - Timeout: 90 seconds with exponential backoff retry policy (Polly)
  - Rate limiting: Handles 429 with Retry-After header respect

**Indexer/Release APIs:**
- **Torznab/Newznab Indexers** - Torrent/NZB search protocol
  - SDK/Client: `TorznabClient` (`src/Services/TorznabClient.cs`)
  - Configuration: Stored in `Indexer` DB table with credentials
  - Client type: Named HttpClient "IndexerClient" with rate limiting and retry policies

- **NZBGet** - Usenet downloader integration
  - SDK/Client: `NzbGetClient` (`src/Services/NzbGetClient.cs`)
  - Configuration: `DownloadClient` DB table
  - Auth: API key via HTTP Basic/custom headers
  - SSL bypass support for self-signed certificates

**TV Schedule/Metadata:**
- **Trash Guides (TRaSH)** - Custom format definitions and quality scoring
  - SDK/Client: `TrashGuideSyncService` (`src/Services/TrashGuideSyncService.cs`)
  - HttpClient: "TrashGuides" with custom user agent
  - Endpoint pattern: Trash Guides GitHub repository
  - Custom formats synced to `CustomFormat` DB table

## Media Server Agents

**Integration Protocol:** Metadata agents (plugins) for media servers

**Emby/MediaBrowser:**
- Location: `agents/emby/Sportarr/`
- Type: C# plugin for Emby/MediaBrowser metadata resolution
- Capabilities: Series/episode metadata provider, image provider
- Configuration: `SportarrPluginOptions.cs` for plugin settings
- Current status: Support for local instance metadata serving

**Jellyfin:**
- Location: `agents/jellyfin/Sportarr/`
- Type: C# plugin for Jellyfin metadata provider
- Capabilities: Series/episode metadata resolution
- Configuration: Plugin-specific options

**Plex:**
- Location: `agents/plex/`
- Status: Agent support (legacy or in-progress)

## Data Storage

**Databases:**
- **SQLite** - Primary data store
  - Provider: Microsoft.EntityFrameworkCore.Sqlite 9.0.9
  - Path: `{DataPath}/sportarr.db`
  - Connection: Via Entity Framework Core DbContext (`SportarrDbContext` in `src/Data/`)
  - Schema: Managed via EF Core migrations (`src/Migrations/`)
  - Entities: Events, Leagues, Teams, Episodes, Downloads, Indexers, Users, Sessions, IPTV channels, DVR recordings, EPG data, etc.

**File Storage:**
- **Local filesystem** - Primary storage
  - Configured root folders: `RootFolder` DB table
  - Event files indexed in: `EventFile` DB table
  - Backup location: Configurable via `config.xml` `BackupFolder`
  - Logs: `{DataPath}/logs/` (rolling file, 10 files, 10MB per file)

**Caching:**
- **Memory Cache** - IMemoryCache (ASP.NET Core in-memory)
  - Sport metadata caching in `SportarrApiClient`
  - Release/search result caching in `SearchResultCache` and `ReleaseCacheService`
  - Custom format match caching in `CustomFormatMatchCache`

## Authentication & Identity

**Auth Provider:**
- **Custom** - Built-in authentication system
  - Implementation approach: API key (default), Basic auth, Forms-based authentication
  - Configuration: `AuthenticationMethod` in `config.xml`
  - Options: "None", "Basic", "Forms"
  - API Key: Stored in `config.xml`, auto-generated on first run
  - Sessions: `AuthSession` DB table for session management
  - Users: `User` DB table for user accounts
  - Password storage: PBKDF2-SHA1 with configurable iterations (default 10000)
  - Implementation: `AuthenticationService` (`src/Services/AuthenticationService.cs`), `SimpleAuthService`, `SessionService`

**Authentication Flows:**
- Endpoint: `AuthEndpoints` (`src/Endpoints/AuthEndpoints.cs`)
- API key verification in middleware (`src/Middleware/`)
- Dynamic authentication scheme based on config

## Monitoring & Observability

**Error Tracking:**
- Not integrated - Errors logged to Serilog only

**Logs:**
- **Serilog** - Structured logging framework
  - Sinks: Console + rolling file
  - Output template: `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}`
  - File: `{DataPath}/logs/sportarr.txt` (rolling by day, 10 files, 10MB limit)
  - Minimum levels:
    - Default: Configured via `LogLevel` in `config.xml` (default: Info)
    - Microsoft framework: Warning level
    - Hosting.Lifetime: Information level
  - Custom formatter: `SanitizingTextFormatter` to protect sensitive data (API keys, passwords)

## CI/CD & Deployment

**Hosting:**
- **Docker** (multi-platform)
  - Base image: `mcr.microsoft.com/dotnet/aspnet:8.0`
  - Architectures: `linux/amd64`, `linux/arm64`
  - Build mechanism: Pre-built binaries from CI
  - Ports: 1867 (HTTP), 1868 (HTTPS optional)
  - Health check: `GET /ping`
  - User: `sportarr` (UID 13001, GID 13001)
  - Volumes: `/config` (data directory)
  - Environment variables: Passthrough for Sportarr__DataPath, timezone, etc.

- **Alternative Deployment:**
  - Standalone Windows executable (system tray support)
  - Standalone Linux binary
  - macOS binary

**CI Pipeline:**
- GitHub Actions (inferred from `.github/` directory)
- Multi-platform builds using Docker buildx
- Pre-built publish outputs copied into Docker image

## Environment Configuration

**Required env vars (for runtime configuration):**
- `Sportarr__DataPath` - Override default data directory
- `Sportarr__ApiKey` - Override or set API key (auto-generated if missing)
- `ASPNETCORE_ENVIRONMENT` - "Production" (default) or "Development"
- `ASPNETCORE_URLS` - HTTP binding URLs (default: `http://*:1867`)

**Secrets location:**
- `config.xml` - API key, authentication credentials, database encryption keys
- Database (`sportarr.db`) - User passwords (PBKDF2-hashed), download client credentials, indexer API keys
- Note: Sensitive values are sanitized in logs via `SanitizingTextFormatter`

**Optional configuration files:**
- `config.xml` - Host settings, authentication, logging, proxy, backup, update, UI preferences, media management
- `.env` / Docker environment - Override env variables
- SSL certificate path: `SslCertPath` in `config.xml`

## Webhooks & Callbacks

**Incoming:**
- **Release/Grab Webhooks** - Incoming webhook endpoints for external sources to trigger event searches/grabs
  - Endpoint: Endpoints in `src/Endpoints/EventSearchAndGrabEndpoints.cs`
  - Mechanism: HTTP POST to configured webhook receivers

- **Sonarr-compatible Webhooks** - For integration with Arr ecosystem tools (Maintainerr, ArrControl, etc.)
  - Endpoint: V3 API endpoints in `src/Endpoints/Sonarr*Endpoints.cs`
  - Protocol: Sonarr/Radarr API compatibility layer

- **Prowlarr Indexer Management** - Indexer notifications
  - Endpoint: V1 API endpoints in `src/Endpoints/V1ProwlarrEndpoints.cs`

**Outgoing:**
- **Notification Service** - Generic notification dispatch
  - Service: `NotificationService` (`src/Services/`)
  - Configuration: `Notification` DB table
  - Types: Configured per notification setup (Discord, webhooks, etc. possible via extensible framework)

- **Event Mapping Sync** - Requests sent to Sportarr-API for mapping status
  - Service: `EventMappingSyncBackgroundService` (`src/Services/EventMappingSyncBackgroundService.cs`)
  - Tracking: `SubmittedMappingRequest` DB table

## Download Clients & Indexers

**Supported Download Clients:**
- Configuration: `DownloadClient` DB table, multiple clients supported
- Client implementations in `src/Services/DownloadClients/`:
  - NZBGet (Usenet)
  - Deluge (Torrent)
  - Custom clients via extensible interface
- Connection validation via health checks

**Indexer Connectivity:**
- Torznab/Newznab protocol support
- RSS feed monitoring background service (`RssSyncService`)
- Rate limiting via `RateLimitHandler` middleware
- Polly retry policy for transient failures

## IPTV/Stream Integration

**IPTV Sources:**
- Configuration: `IptvSource` DB table
- Xtream Codes IPTV protocol support (`XtreamCodesClient`)
- M3U playlist parsing (`M3uParserService`)
- Channel mapping to leagues (`ChannelLeagueMapping` DB table)

**EPG (Electronic Program Guide):**
- XMLTV format parsing (`XmltvParserService`)
- Sources: `EpgSource` DB table
- Channels: `EpgChannel` DB table
- Programs: `EpgProgram` DB table
- Download client: "EpgClient" with 5-minute timeout for large gzipped feeds

**DVR Recording:**
- FFmpeg-based recording (`FFmpegRecorderService`, `DvrRecordingService`)
- Recording scheduling (`DvrSchedulerService`, `DvrAutoSchedulerService`)
- Status tracking: `DvrRecording` DB table
- Quality profiles: `DvrQualityProfile` DB table

## HDHomeRun Emulation

**Live TV Discovery:**
- Endpoint: `/discover.json` and other SiliconDust HTTP API contract endpoints
- Allows Plex DVR, Jellyfin Live TV, Emby, Channels DVR to discover Sportarr's IPTV channels as network tuner
- Implementation: `src/Endpoints/HdHomeRunEndpoints.cs`

## Cross-Service Compatibility

**Sonarr API Shims:**
- Location: Multiple endpoints in `src/Endpoints/Sonarr*Endpoints.cs` and `src/Endpoints/V1ProwlarrEndpoints.cs`
- Purpose: Allow Arr ecosystem tools (Prowlarr, Decypharr, Maintainerr, ArrControl) to manage Sportarr as if it were Sonarr
- Compatibility: `/api/v1/*` (Prowlarr), `/api/v3/*` (Decypharr/Maintainerr/ArrControl)
- Mapped endpoints: System, Command, Series, Calendar, EpisodeFile, Config, Indexer, DownloadClient

---

*Integration audit: 2026-06-01*
