# Codebase Concerns

**Analysis Date:** 2026-06-01

## Tech Debt

**Unfinished Quality Profile Cutoff Check:**
- Issue: The quality profile cutoff feature for the "wanted/cutoff-unmet" endpoint is incomplete
- Files: `src/Endpoints/BlocklistAndWantedEndpoints.cs:132`
- Impact: Users cannot reliably identify episodes that are below their quality cutoff threshold, making targeted upgrades impossible
- Fix approach: Implement quality score comparison against QualityProfile cutoff thresholds; compare file quality against profile targets

**Deprecated Configuration Fields:**
- Issue: `Config.DvrDefaultProfileId` is marked DEPRECATED and superseded by encoding settings, but code still references it in some places
- Files: `src/Models/Config.cs:163`
- Impact: Legacy configurations may behave unexpectedly; new settings may be ignored if old field is consulted
- Fix approach: Remove all references to DvrDefaultProfileId; migrate existing configs to new encoding-based approach; verify no read-paths still check the old field

**Deprecated Type Definitions in Frontend:**
- Issue: `FightCard` interface and related part-status fields are marked DEPRECATED but still present in type definitions for backwards compatibility
- Files: `frontend/src/types/index.ts:39,96`
- Impact: Code paths may branch on two different representations of the same concept; new features must support both old and new patterns
- Fix approach: Full cutover to `PartStatus` interface; remove FightCard; audit all consuming code for dead branches

**Legacy TSDB ID Migration Logic:**
- Issue: Complex league/team ID migration from numeric TheSportsDB IDs to hub short_ids (lg-XXXXXX format) is ongoing
- Files: `src/Startup/DatabaseInitializer.cs:89-96`, `src/Services/LeagueEventSyncService.cs:107-110,660-661,882`
- Impact: Dual-ID handling in sync logic increases maintenance burden; orphaned legacy leagues may not sync properly until next rebuild
- Fix approach: Establish firm cutoff date for legacy ID support; batch-migrate remaining legacy leagues; remove dual-ID code paths

## Security Vulnerabilities

**SQL Injection in Schema Repair Code:**
- Issue: Dynamic column name interpolation into SQL strings without parameterization in database initializer
- Files: `src/Startup/DatabaseInitializer.cs:301,306,331,336`
- Code: `$"SELECT COUNT(*) FROM pragma_table_info('ChannelLeagueMappings') WHERE name='{col}'"` and `$"ALTER TABLE ChannelLeagueMappings ADD COLUMN {col} {type}"`
- Impact: Malformed schema repair could fail silently; intentional injection could alter schema unexpectedly
- Fix approach: Use parameterized queries or escape column/type identifiers; or verify against whitelist of known column names before interpolation

**Potential Path Traversal:**
- Issue: File paths from API endpoints (`/api/library/scan`, `/api/library/preview`) are passed directly to `Path.Combine` without normalization or traversal checks
- Files: `src/Endpoints/LibraryEndpoints.cs:15,42`, `src/Services/LibraryImportService.cs:578,643`
- Impact: User with API access could traverse parent directories (../../../) to list/import files outside intended media folders
- Fix approach: Validate path is within allowed root folder boundaries before opening; use `Path.GetFullPath` and verify no `..` escapes allowed roots

**Unvalidated Task.Run Fire-and-Forget:**
- Issue: Multiple endpoints spawn background tasks with `_ = Task.Run(async () => {...})` which silently fail if exceptions occur
- Files: `src/Endpoints/EventEndpoints.cs:371`, `src/Endpoints/SystemBackupEndpoints.cs:140`, `src/Endpoints/LeagueEndpoints.cs:1343`
- Impact: Background search/backup operations could fail silently; user sees no error indication; operations silently abandon
- Fix approach: Wrap Task.Run operations with try-catch that logs to logger and sends user notification; consider using hosted service instead

**Unsafe Null Handling in Import Service:**
- Issue: `null!` operator used to suppress null-safety warnings without actual null checks
- Files: `src/Services/FileImportService.cs:346`, `src/Services/PackImportService.cs:522`
- Impact: If null value is set, code will throw NullReferenceException at runtime instead of compile-time safety
- Fix approach: Remove `null!` declarations; properly initialize object or validate non-null before use

## Known Issues

**Array.First() Without Guards:**
- Issue: Multiple `.First()` calls on collections without prior `.Any()` check
- Files: `src/Endpoints/EventSearchAndGrabEndpoints.cs:350`, `src/Endpoints/EventEndpoints.cs:336`, `src/Services/FileImportService.cs:256,1427`, `src/Services/SportarrApiClient.cs:852,921`, `src/Services/LibraryImportService.cs:718`, `src/Services/IndexerSearchService.cs:563,570`, `src/Services/ReleaseCacheService.cs:56`, `src/Services/ReleaseMatchScorer.cs:1434`
- Trigger: When collection is empty (no results from search, no video files found, no root folders configured)
- Workaround: Endpoint returns 500 error; user must check logs or retry
- Fix approach: Use `.FirstOrDefault()` with null check, or `.FirstOrDefault(null)` with explicit fallback logic

**Incomplete Error Tracking Integration:**
- Issue: ErrorBoundary in frontend has placeholder for Sentry integration but does not actually send errors
- Files: `frontend/src/components/ErrorBoundary.tsx:51`
- Symptoms: Application errors in production are logged to browser console only; no centralized error tracking
- Impact: Production errors may go unnoticed; debugging customer issues is difficult
- Fix approach: Implement Sentry (or equivalent) integration; configure with appropriate environment detection

## Performance Bottlenecks

**Large File Operations Without Batching:**
- Issue: DatabaseInitializer performs schema repairs row-by-row in fallback migration seeding
- Files: `src/Startup/DatabaseInitializer.cs:55-67`
- Cause: Loop inserts each migration individually into `__EFMigrationsHistory` table; no batch insert or transaction
- Impact: Database initialization on large legacy installs (hundreds of migrations) becomes slow; repeated database round-trips
- Improvement path: Batch insert migrations in single SQL statement; wrap in explicit transaction

**Excessive Console.Log in Production Code:**
- Issue: 243 console.log statements found in frontend, many marked as DEBUG but not removed
- Files: `frontend/src/pages/settings/DownloadClientsSettings.tsx:78,201,203,409,410,450,451,454`
- Cause: Left in during development; conditionally logged only in dev environment but bloats bundle and impacts performance
- Impact: Debug output persists in production builds; minor performance overhead; reveals internal state to users
- Improvement path: Remove all DEBUG/console.log statements or gate behind `if (import.meta.env.DEV)` guards

**Polling-Based Task Monitoring:**
- Issue: TaskQueueFooter polls `/api/task` every 2 seconds regardless of whether user is viewing the page
- Files: `frontend/src/components/TaskQueueFooter.tsx:25`
- Cause: setInterval runs continuously; no debouncing or visibility detection
- Impact: Unnecessary API calls even when page is hidden; network and server resource waste
- Improvement path: Use Page Visibility API to pause polling when tab is not visible; consider WebSocket for real-time updates

**Large Service Files Mixing Concerns:**
- Issue: Core service files exceed 1600 lines and handle multiple distinct concerns
- Files: `src/Services/FileImportService.cs:1752`, `src/Services/LibraryImportService.cs:1666`, `src/Services/AutomaticSearchService.cs:1650`, `src/Services/ReleaseMatchScorer.cs:1615`, `src/Services/LeagueEventSyncService.cs:1595`
- Cause: Services grew organically; no refactoring boundary established
- Impact: Difficult to understand flow; hard to test in isolation; high complexity increases bug probability
- Improvement path: Extract distinct concerns into separate classes (e.g., separate `ReleaseEvaluationStrategy`, `FileNamingStrategy`)

## Fragile Areas

**Database Schema Safety Nets:**
- Files: `src/Startup/DatabaseInitializer.cs:75-90`
- Why fragile: Schema repair logic depends on implicit ordering; if a later safety net reads from a column that wasn't added yet, it silently fails, leaving schema partially broken
- Safe modification: Always add new column checks BEFORE any code that reads from that column; test with fresh install and legacy database
- Test coverage: No integration tests for database upgrade paths; manual testing only

**Event-to-File Mapping in Multi-Part Episodes:**
- Files: `src/Services/EventPartDetector.cs`, `src/Models/Event.cs` (MonitoredParts field)
- Why fragile: Transition from FightCard to PartStatus representation; code must handle both legacy and new formats; part names aren't validated
- Safe modification: Ensure all part-status code paths handle empty/null parts gracefully; add validation for part name format
- Test coverage: No tests for malformed part names; missing tests for empty part list scenarios

**Download Client Protocol Detection:**
- Files: `frontend/src/pages/settings/DownloadClientsSettings.tsx:76-79`
- Why fragile: Protocol determined by client type enum value; if new client types are added, protocol detection must be updated manually in multiple places
- Safe modification: Add unit test for each client type; document mapping in constants; refactor to enum-driven approach
- Test coverage: No tests for protocol detection; manual UI testing only

**Fire-and-Forget Background Tasks:**
- Files: `src/Endpoints/EventEndpoints.cs:371,531`, `src/Services/AutomaticSearchService.cs`
- Why fragile: No way to track status or retry failed operations; database state may become inconsistent if task fails silently
- Safe modification: Never spawn fire-and-forget tasks from endpoints; use a proper task queue service (e.g., Hangfire, Quartz); always wrap in try-catch with logging
- Test coverage: Background operations are never tested; production behavior unknown

**Legacy IPTV/DVR Auto-Mapping:**
- Files: `src/Startup/DatabaseInitializer.cs:294-317` (ChannelLeagueMappings scoring)
- Why fragile: Complex scoring logic without documented rules; changes to scoring thresholds could silently break existing mappings
- Safe modification: Add integration test that validates scoring before/after changes; document scoring algorithm; add debug logging
- Test coverage: No tests for mapping algorithm; no validation of scoring thresholds

## Scaling Limits

**SQLite Concurrent Write Limitation:**
- Current capacity: Single writer at a time (WAL mode partially mitigates)
- Limit: Multi-user installations with simultaneous operations (e.g., two users importing files while background sync runs) will hit "database is locked" errors
- Scaling path: Migrate to PostgreSQL for production; introduce async queue for heavy operations (file imports, searches)

**In-Memory Caching Without Limits:**
- Files: Various services maintain in-memory collections without size limits
- Limit: Long-running instances may exhaust memory with large leagues (thousands of events, teams)
- Scaling path: Implement cache eviction (LRU or TTL); limit cache size; use database queries instead of in-memory filters

**RSS Sync at 500 Releases per Indexer:**
- Files: `src/Models/Config.cs:139`
- Limit: If indexer RSS feed returns >500 releases, older releases are silently dropped; could miss older events
- Scaling path: Implement pagination or timestamp-based filtering; increase default limit or make configurable per indexer

## Missing Critical Features

**Quality Profile Cutoff Enforcement:**
- Problem: "Cutoff unmet" detection is incomplete; no way to find episodes below target quality
- Blocks: Upgrade search feature; quality-aware automatic searching
- Impact: Users cannot reliably maintain minimum quality standards

**Error Tracking in Production:**
- Problem: Application errors are logged to console only; no centralized error tracking
- Blocks: Identifying production issues; correlating errors across user base
- Impact: Production bugs may go unnoticed for days

## Test Coverage Gaps

**Database Migration Path:**
- What's not tested: Upgrade path from EnsureCreated legacy database to new migrations; schema repair safety nets
- Files: `src/Startup/DatabaseInitializer.cs`
- Risk: Schema corruption on upgrade could silently corrupt user data
- Priority: High

**Background Task Error Handling:**
- What's not tested: Fire-and-forget tasks; exception handling in background operations
- Files: `src/Endpoints/EventEndpoints.cs:371,531`, `src/Endpoints/SystemBackupEndpoints.cs:140`, `src/Endpoints/LeagueEndpoints.cs:1343`
- Risk: Silent failures in search/backup operations; user unaware of failures
- Priority: High

**Path Traversal Security:**
- What's not tested: API endpoints that accept file paths as input
- Files: `src/Endpoints/LibraryEndpoints.cs:15,42`, `src/Services/LibraryImportService.cs`
- Risk: Unauthorized access to files outside intended directories
- Priority: High

**Protocol Detection:**
- What's not tested: Download client protocol detection logic
- Files: `frontend/src/pages/settings/DownloadClientsSettings.tsx:76-79`
- Risk: Incorrect client configuration; protocol mismatch
- Priority: Medium

**Multi-Part Episode Handling:**
- What's not tested: Legacy FightCard to PartStatus migration; malformed part data
- Files: `src/Services/EventPartDetector.cs`, `src/Models/Event.cs`
- Risk: Episodes with parts not detected correctly; partial data loss
- Priority: Medium

**IPTV Channel Mapping Scoring:**
- What's not tested: Channel-to-league auto-mapping algorithm; scoring thresholds
- Files: `src/Startup/DatabaseInitializer.cs:294-317`
- Risk: Wrong channels mapped to wrong leagues; user sees incorrect EPG
- Priority: Medium

---

*Concerns audit: 2026-06-01*
