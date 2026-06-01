# Sportarr — IPTV & EPG Scheduled Auto-Refresh

## What This Is

A milestone within Sportarr (a Sonarr-style sports PVR built on ASP.NET Core 8 + React) that adds **configurable, hours-based scheduled auto-refresh** for IPTV sources (M3U / Xtream Codes) and EPG sources (XMLTV). Today both are fetched only manually — IPTV syncs once on add then never again, and EPG isn't even fetched on add. This work makes channel lists and guide data stay current on a user-configured cadence, and brings EPG source management up to parity with IPTV (advanced settings UI, non-blocking sync on add).

## Core Value

IPTV channel lists and EPG guide data refresh automatically on a per-source, user-configured interval — without anyone clicking "Sync" — while never blocking the UI and never generating provider traffic the user didn't opt into.

## Requirements

### Validated

<!-- Inferred from existing code — already shipped and relied upon. -->

- ✓ IPTV sources (M3U + Xtream) can be added, edited, deleted, and manually synced — existing (`IptvSourceService`, `IptvEndpoints`)
- ✓ IPTV source Advanced Settings expose MaxStreams and UserAgent — existing (`Models/Iptv.cs`, `IptvSettings.tsx`)
- ✓ EPG sources (XMLTV) can be added, edited, deleted, and manually synced — existing (`EpgService`, `EpgEndpoints`)
- ✓ EPG sync parses large/gzip XMLTV (5-min timeout), bulk-loads programs, and auto-maps IPTV→EPG channels by name — existing (`EpgService.SyncSourceAsync`, `XmltvParserService`)
- ✓ Manual sync endpoints exist for both subsystems (`POST /api/iptv/sources/{id}/sync`, `POST /api/epg/sources/{id}/sync`) — existing
- ✓ Background-service pattern with interval config exists as prior art (`RssSyncService`, `TrashSyncBackgroundService` with `AutoSyncIntervalHours`) — existing

### Active

<!-- This milestone. Building toward these. -->

- [ ] Each IPTV source has a configurable refresh interval (hours) in its Advanced Settings; `0`/empty = disabled
- [ ] Each EPG source has a configurable refresh interval (hours); `0`/empty = disabled
- [ ] A single shared background worker periodically refreshes any source whose interval has elapsed (based on `LastUpdated`)
- [ ] Adding an EPG source triggers a first, **non-blocking** sync (background worker picks it up) instead of leaving it empty until manual sync
- [ ] EPG sources get an advanced settings / edit UI (mirroring the IPTV Advanced Settings UX) exposing the interval field
- [ ] IPTV settings UI exposes the new interval field alongside MaxStreams / UserAgent
- [ ] Existing sources are unaffected until an interval is set (opt-in; no surprise provider traffic)

### Out of Scope

- Default-on auto-refresh — defaults to disabled to avoid surprise provider traffic and respect rate limits
- Removing or replacing the existing manual-sync endpoints — they remain and are reused by the worker
- Per-source dedicated timers — a single shared worker is simpler and restart-safe
- Redesigning the M3U / XMLTV parsing pipeline — reuse `M3uParserService` / `XmltvParserService` as-is
- Sub-hour / cron-style schedules — hours-based interval only for this milestone
- Inline blocking sync on EPG add — deliberately avoided (large payloads, 5-min timeout would hang the request)

## Context

- **Brownfield.** Mature codebase mapped in `.planning/codebase/` (ASP.NET Core 8, EF Core + SQLite, React 19 + Vite + React Query + Tailwind). New work follows established conventions documented there.
- **Why EPG isn't synced on add today (investigated, not guessed):** XMLTV files can be hundreds of MB (`XmltvParserService.cs:44` sets a 5-minute timeout, handles gzip). Syncing inline on the Add request — as IPTV does for its lighter M3U — would hang the request for minutes and risk timeouts. Auto-mapping is also only meaningful after IPTV channels exist. Most likely a combination of "deliberately decoupled to keep Add fast" and "alpha feature, scheduling never finished." This milestone resolves it the right way: heavy syncs run in the background, never on the request.
- **Self-healing mapping:** because the worker re-runs `AutoMapChannelsAsync` each cycle, the "add IPTV before EPG" ordering problem resolves over time automatically.
- **Prior art to model:** `RssSyncService` (interval-driven RSS polling) and `TrashSyncBackgroundService` (`AutoSyncIntervalHours`) are the closest existing patterns for an interval-driven background worker.
- Originated from a branch (`support-game-thumbs`) of unrelated Emby agent work; this milestone is a separate concern.

## Constraints

- **Tech stack**: Must fit existing stack — EF Core migration for new columns, `BackgroundService` registered via `AddSportarrBackgroundServices()`, FluentValidation for request DTOs, React Query hooks + Tailwind for UI.
- **Compatibility**: Existing IPTV/EPG sources must keep working unchanged; new interval column defaults to disabled.
- **Performance / etiquette**: Refresh must respect provider load — opt-in only, hours-granularity, single worker staggering writes (SQLite single-writer limitation noted in codebase concerns).
- **Non-blocking**: Source Add/Edit HTTP requests must not block on a full sync.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Per-source interval (not global) | Matches how the user described it; different providers warrant different cadences | — Pending |
| Opt-in / off by default (`0` = disabled) | Avoid surprise provider traffic; preserve existing behavior until configured | — Pending |
| Single shared `BackgroundService` over per-source timers | Mirrors existing RssSync/TrashSync pattern; restart-safe; fewer moving parts | — Pending |
| EPG sync on add is non-blocking (background) | XMLTV payloads are huge (5-min timeout); honors original author's intent; no hung Add request | — Pending |
| Build full EPG advanced-settings UI | EPG currently has no edit UI; per-source interval config needs a form to be usable | — Pending |
| Reuse existing manual-sync code paths | `SyncChannelsAsync` / `SyncSourceAsync` already do the work; worker just schedules them | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-01 after initialization*
