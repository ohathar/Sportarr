# Codebase Structure

**Analysis Date:** 2026-06-01

## Directory Layout

```
Sportarr/
├── src/                            # .NET backend
│   ├── Program.cs                  # Startup orchestration (960 lines)
│   ├── Sportarr.csproj            # Project file with frontend build target
│   ├── Authentication/             # Auth handlers (ApiKey, Basic, Forms)
│   ├── Constants/                  # Magic-string-free constants
│   ├── Converters/                 # JSON serialization converters
│   ├── Data/                       # SportarrDbContext, EF Core setup
│   ├── Endpoints/                  # 45 HTTP endpoint groups (one class per domain)
│   ├── Exceptions/                 # Custom exception types
│   ├── Health/                     # Health check implementations
│   ├── Helpers/                    # Pure static utility functions
│   ├── Middleware/                 # ASP.NET middleware (CORS, auth, logging)
│   ├── Migrations/                 # EF Core migration files
│   ├── Models/                     # Domain entities + request/response DTOs
│   │   └── Requests/               # Inline request records
│   │   └── Metadata/               # Metadata API types
│   ├── Services/                   # 90+ business logic services
│   │   ├── DownloadClients/        # Download client adapters
│   │   └── Interfaces/             # Service interfaces
│   ├── Startup/                    # DI registration, database init, agent install
│   ├── Validation/                 # FluentValidation custom validators (legacy)
│   ├── Validators/                 # Request DTO validators (FluentValidation)
│   ├── Windows/                    # Windows-specific code (tray, console hide)
│   └── wwwroot/                    # Static files (frontend build output)
│
├── frontend/                       # React + TypeScript web UI
│   ├── src/
│   │   ├── App.tsx                 # Root component
│   │   ├── main.tsx                # Entry point
│   │   ├── api/                    # HTTP client + React Query hooks
│   │   ├── assets/                 # Images, logos, static files
│   │   ├── components/             # Reusable React components
│   │   ├── contexts/               # Context providers (auth, theme, etc.)
│   │   ├── hooks/                  # Custom React hooks
│   │   ├── pages/                  # Page components (routed views)
│   │   │   ├── settings/           # Settings pages
│   │   │   └── iptv/               # IPTV/DVR pages
│   │   ├── test/                   # Test utilities
│   │   ├── types/                  # TypeScript type definitions
│   │   └── utils/                  # Helper functions
│   ├── vite.config.ts              # Vite build config
│   ├── vitest.config.ts            # Vitest test config
│   ├── tsconfig.json               # TypeScript config
│   └── package.json                # Node dependencies
│
├── agents/                         # Media server agent plugins
│   ├── plex/
│   │   ├── Sportarr-Legacy.bundle/ # Legacy Plex plugin (deprecated)
│   │   └── README.md
│   ├── jellyfin/
│   │   ├── Sportarr/               # .NET plugin (Jellyfin SDK)
│   │   └── README.md
│   ├── emby/
│   │   ├── Sportarr/               # .NET plugin (Emby SDK)
│   │   └── README.md
│   └── README.md
│
├── docs/                           # Documentation
│   ├── ARCHITECTURE.md             # Architectural patterns
│   ├── api.md                      # API endpoint reference
│   ├── API_VERSIONING.md           # API versioning strategy
│   ├── plex-metadata-provider-api.md
│   └── images/                     # Screenshots
│
├── tests/                          # Test suite
│   ├── Sportarr.Api.Tests/         # Unit tests for parsers, evaluators
│   └── README.md
│
├── .devcontainer/                  # VS Code devcontainer config
├── .github/                        # GitHub Actions workflows
├── .planning/
│   └── codebase/                   # GSD codebase analysis documents
├── .vscode/                        # VS Code workspace settings
├── installer/                      # Windows installer packaging
├── Logo/                           # Branding assets (256.png, etc.)
├── README.md                       # Project overview
├── COPYRIGHT.md                    # License info
├── SECURITY.md                     # Security policy
└── CLA.md                          # Contributor agreement
```

## Directory Purposes

**`src/`** — .NET backend server
- Purpose: Core application logic, HTTP API, database context, business services
- Contains: C# source code, EF Core migrations, endpoint handlers
- Key files: `Program.cs` (startup), `Data/SportarrDbContext.cs` (ORM), `Endpoints/*` (routes), `Services/*` (logic)

**`frontend/`** — React TypeScript web UI
- Purpose: User-facing web application
- Contains: React components, TypeScript types, Vite build config, vitest tests
- Key files: `src/App.tsx` (root), `src/pages/` (routed pages), `src/api/hooks.ts` (API client)

**`agents/`** — Media server plugins
- Purpose: Metadata providers for Plex, Jellyfin, Emby
- Contains: 
  - Plex: Legacy bundle (deprecated 2026)
  - Jellyfin: .NET plugin (active)
  - Emby: .NET plugin (active)
- Separate projects; built independently

**`docs/`** — Reference documentation
- Purpose: API specifications, architectural patterns, setup guides
- Key files: `ARCHITECTURE.md`, `api.md`, `API_VERSIONING.md`

**`tests/`** — Test suite
- Purpose: Unit tests for parsers, evaluators, validators
- Contains: xUnit test classes for sports file parsing, release evaluation
- Coverage: ~95% on core parsers; integration test infrastructure pending

**`.planning/codebase/`** — GSD analysis documents
- Purpose: Generated by `/gsd-map-codebase` for phase planning
- Contains: ARCHITECTURE.md, STRUCTURE.md, CONVENTIONS.md, TESTING.md, CONCERNS.md

## Key File Locations

**Entry Points:**
- `src/Program.cs` — Application startup, DI setup, middleware pipeline (~960 lines)
- `frontend/src/main.tsx` — React app bootstrap
- `agents/*/Sportarr*.csproj` — Agent plugin entry points

**Configuration:**
- `src/Sportarr.csproj` — .NET project metadata, NuGet dependencies, frontend build target
- `frontend/vite.config.ts` — React build configuration
- `frontend/tsconfig.json` — TypeScript compiler settings
- `frontend/package.json` — Node.js dependencies

**Core Logic:**
- `src/Data/SportarrDbContext.cs` — EF Core context, 50+ DbSet definitions
- `src/Services/IndexerSearchService.cs` — Multi-indexer search orchestration
- `src/Services/ReleaseEvaluator.cs` — Quality scoring engine
- `src/Services/LeagueEventSyncService.cs` — Event sync from Sportarr API
- `src/Services/FileImportService.cs` — Downloaded file matching & import
- `src/Services/DvrRecordingService.cs` — IPTV DVR scheduling

**HTTP Endpoints:**
- `src/Endpoints/EventSearchAndGrabEndpoints.cs` — Search, grab, manual events
- `src/Endpoints/EventEndpoints.cs` — Event CRUD and status
- `src/Endpoints/IptvEndpoints.cs` — IPTV sources, channels, DVR
- `src/Endpoints/DownloadClientEndpoints.cs` — Download client management
- `src/Endpoints/IndexerEndpoints.cs` — Indexer configuration
- `src/Endpoints/SettingsEndpoints.cs` — Application settings

**Database:**
- `src/Models/Event.cs` — Event entity (date, league, teams, status)
- `src/Models/League.cs` — League entity (name, sport, sync status)
- `src/Models/EventFile.cs` — File mapping to event (renamed path, quality)
- `src/Models/DownloadQueueItem.cs` — In-flight download tracking
- `src/Migrations/*.cs` — EF Core migration history

**Testing:**
- `tests/Sportarr.Api.Tests/` — xUnit test project
- Test files: `*ParserTests.cs`, `*EvaluatorTests.cs`, `*HelperTests.cs`

**Frontend:**
- `frontend/src/pages/EventsPage.tsx` — Events library view
- `frontend/src/pages/LeaguesPage.tsx` — League management
- `frontend/src/pages/ActivityPage.tsx` — Download/import queue
- `frontend/src/pages/settings/` — Configuration pages
- `frontend/src/api/hooks.ts` — All API client functions (React Query hooks)
- `frontend/src/components/` — Shared UI components (modals, tables, forms)

## Naming Conventions

**Files:**
- Endpoints: `{Domain}Endpoints.cs` (e.g., `EventEndpoints.cs`, `IptvEndpoints.cs`)
- Services: `{Concern}Service.cs` (e.g., `IndexerSearchService.cs`, `FileImportService.cs`)
- Validators: `{Request}Validator.cs` (e.g., `AddEventRequestValidator.cs`)
- Models: `{EntityName}.cs` (e.g., `Event.cs`, `League.cs`)
- Helpers: `{Concern}Helper.cs` (e.g., `PartRelevanceHelper.cs`, `TennisLeagueHelper.cs`)
- Download clients: `{Client}Client.cs` (e.g., `QBittorrentClient.cs`, `SabnzbdClient.cs`)
- React pages: `{Name}Page.tsx` (e.g., `EventsPage.tsx`, `LeaguesPage.tsx`)
- React components: `{Name}.tsx` (PascalCase, e.g., `EventCard.tsx`, `SearchModal.tsx`)

**Directories:**
- C# namespaces mirror directory paths: `src/Services/Interfaces/` → `namespace Sportarr.Api.Services.Interfaces`
- Endpoints grouped by domain: `Endpoints/EventEndpoints.cs`, `Endpoints/IptvEndpoints.cs`
- Services grouped by feature: all indexer logic in `Services/IndexerSearchService.cs` + related, all IPTV in `Services/Iptv*.cs`
- React: Feature-based (`pages/iptv/`, `components/shared/`, `hooks/useAuth.ts`)

## Where to Add New Code

**New HTTP Endpoint:**
1. Determine domain: Is it events, indexers, IPTV, download clients, settings?
2. Check if file exists: e.g., `src/Endpoints/EventEndpoints.cs`
3. If it exists, add route handler inside the `MapXxxEndpoints()` method
4. If new file needed: Create `src/Endpoints/NewDomainEndpoints.cs` following the template in `docs/ARCHITECTURE.md`
5. Register in `Program.cs` with one-liner: `app.MapNewDomainEndpoints();`
6. Write validator in `src/Validators/` if endpoint has a POST/PUT body
7. Wire validator to endpoint: `.WithRequestValidation<YourRequestDto>()`

**New Service:**
1. Create file `src/Services/YourService.cs`
2. Inject `ILogger<YourService>` and any dependencies
3. Register in `Startup/ServiceCollectionExtensions.cs` in the appropriate extension method:
   - Core services → `AddSportarrCoreServices`
   - Indexing logic → `AddSportarrIndexing`
   - File operations → `AddSportarrFileServices`
   - IPTV → `AddSportarrIptv`
   - Background workers → `AddSportarrBackgroundServices` (if implements `BackgroundService`)
4. Inject into endpoints or other services by type

**New Background Service:**
1. Create file `src/Services/YourBackgroundService.cs`
2. Inherit from `BackgroundService`
3. Implement `ExecuteAsync(CancellationToken)` with main loop
4. Register in `AddSportarrBackgroundServices()` via `services.AddHostedService<YourBackgroundService>()`
5. Respect cancellation token — exit gracefully on shutdown

**New Database Model:**
1. Create file `src/Models/YourEntity.cs` with properties and `[Key]` attribute
2. Add `DbSet<YourEntity> YourEntities` to `src/Data/SportarrDbContext.cs`
3. Configure in `OnModelCreating()` if custom mapping needed (max lengths, foreign keys, etc.)
4. Create EF Core migration: `dotnet ef migrations add AddYourEntity`
5. Apply migration: `dotnet ef database update`

**New React Page:**
1. Create file `frontend/src/pages/YourPage.tsx`
2. Export default React component
3. Add route to `frontend/src/App.tsx` in the router config
4. Call API via hooks from `frontend/src/api/hooks.ts` (already has all endpoints)
5. If new endpoint type, add hook to `hooks.ts` using `useQuery` or `useMutation`

**New Validator:**
1. Create file `src/Validators/YourRequestValidator.cs`
2. Inherit from `AbstractValidator<YourRequestDto>`
3. Define rules in constructor: `RuleFor(x => x.Property).NotEmpty().MaximumLength(100)`
4. Auto-registered via `AddValidatorsFromAssembly()` — no manual registration
5. Apply to endpoint: `.WithRequestValidation<YourRequestDto>()`

**Utility/Helper Function:**
1. If stateless with no DI needs: Add to `src/Helpers/` as static class/method
2. If needs logging, accept `ILogger<T>` as parameter
3. If complex stateful logic: Make it a service, not a helper

**Download Client Adapter:**
1. Create file `src/Services/{ClientName}Client.cs` (e.g., `UtorrentClient.cs`)
2. Inherit from `BaseDownloadClient`
3. Implement required methods: `AddAsync`, `GetStatusAsync`, `RemoveAsync`, etc.
4. Register in `DownloadClientService.GetAdapter()` switch statement
5. Document in `README.md` → Supported Download Clients section

## Special Directories

**`src/Migrations/`:**
- Purpose: EF Core migration history
- Generated: Yes (via `dotnet ef migrations add`)
- Committed: Yes (part of source control)
- Never edit manually — use `dotnet ef` CLI

**`src/wwwroot/`:**
- Purpose: Static files served by ASP.NET (frontend build output, assets)
- Generated: Yes (copied by csproj `BuildFrontend` target from `_output/UI`)
- Committed: No (always rebuilt)
- Do not edit manually — regenerate via `npm run build` in `frontend/`

**`frontend/node_modules/`:**
- Purpose: npm dependencies
- Generated: Yes (via `npm install` or `npm ci`)
- Committed: No (use package-lock.json for reproducibility)
- Do not edit manually

**`src/obj/` and `src/bin/`:**
- Purpose: .NET build artifacts
- Generated: Yes (via `dotnet build`)
- Committed: No
- Do not edit manually

**`agents/*/bin/` and `agents/*/obj/`:**
- Purpose: Agent plugin build output
- Generated: Yes (via `dotnet build` in agent folders)
- Committed: No
- Do not edit manually

**`_output/UI/`:**
- Purpose: Intermediate React build output
- Generated: Yes (via `npm run build` in `frontend/`)
- Committed: No
- Do not edit manually — serves as staging for `wwwroot/` copy

---

*Structure analysis: 2026-06-01*
