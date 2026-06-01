# Technology Stack

**Analysis Date:** 2026-06-01

## Languages

**Primary:**
- C# - Backend API and core application logic in `src/`
- TypeScript - Frontend React application in `frontend/`
- HTML/CSS - UI templates and styling

**Secondary:**
- Shell - Docker entrypoint script (`docker-entrypoint.sh`)
- XML - Configuration files (`config.xml`)
- JSON - Configuration and data serialization

## Runtime

**Environment:**
- .NET 8.0 (with Windows-specific `net8.0-windows` for tray support on Windows)
- Node.js 20+ (for frontend build)

**Package Manager:**
- NuGet - .NET packages (referenced in `src/Sportarr.csproj`)
- npm - Node.js packages (frontend dependencies in `frontend/package.json`)
- Lockfile: `package-lock.json` present

## Frameworks

**Core:**
- ASP.NET Core 8.0 - Web framework and HTTP API server
- Entity Framework Core 9.0.9 - ORM for database access (`src/Data/SportarrDbContext.cs`)
- React 19.1.1 - Frontend UI framework
- Vite 6.3.6 - Frontend build tool and dev server

**Testing:**
- Vitest 4.0.14 - Frontend unit testing (`frontend/vitest.config.ts`)
- xUnit/MSTest - Backend testing (inferred from `tests/Sportarr.Api.Tests/`)

**Build/Dev:**
- Vite 6.3.6 - Frontend bundler and dev server
- TypeScript 5.9.3 - Type-safe frontend development
- ESLint 9.36.0 - Frontend linting
- Tailwind CSS 3.4.18 - Utility-first CSS framework
- PostCSS 8.5.6 - CSS transformations

## Key Dependencies

**Critical:**
- Microsoft.AspNetCore.OpenApi 8.0.19 - OpenAPI/Swagger documentation
- Serilog 8.0.3 - Structured logging framework with file and console sinks
- SQLitePCLRaw.bundle_sqlite3 2.1.10 - System SQLite binding (avoids CPU opcode compatibility issues)
- FluentValidation 11.10.0 - Request validation framework
- Polly 9+ - Resilience and retry policies for HTTP clients
- Swashbuckle.AspNetCore 9.0.6 - Swagger/OpenAPI UI

**Infrastructure:**
- Microsoft.EntityFrameworkCore.Sqlite 9.0.9 - SQLite database provider
- Ical.Net 4.3.1 - iCalendar format parsing/generation
- FuzzySharp 2.0.2 - Fuzzy string matching for resolvers
- System.Text.Encoding.CodePages 9.0.0 - Legacy character encoding support

**Frontend:**
- @tanstack/react-query 5.90.3 - Server state management and caching
- Axios 1.13.5 - HTTP client for API calls
- hls.js 1.6.15 - HLS video stream playback
- mpegts.js 1.8.0 - MPEG-TS video stream playback
- react-router-dom 7.12.0 - Client-side routing
- @headlessui/react 2.2.9 - Accessible UI components
- @heroicons/react 2.2.0 - Icon library
- sonner 1.4.0 - Toast notification library

## Configuration

**Environment:**
- Configuration via `config.xml` (XML format stored in data directory)
- Environment variables:
  - `Sportarr__DataPath` - Data directory override (defaults to `./data` or platform-specific locations)
  - `Sportarr__ApiKey` - API key for external access (auto-generated if missing)
  - `ASPNETCORE_ENVIRONMENT` - Environment mode (Production default)
  - `DOTNET_CLI_TELEMETRY_OPTOUT` - Disable telemetry (set to 1 by default)
  - `SPORTARR_BRANCH` - Git branch identifier
  - `LIBVA_DRIVER_NAME` - GPU driver selection (iHD for Intel, i965 for legacy, radeonsi for AMD)

**Build:**
- `src/Sportarr.csproj` - .NET project configuration with frontend build integration
- `frontend/vite.config.ts` - Vite configuration with API proxy to localhost:1867
- `frontend/tsconfig.json` - TypeScript compiler options
- `frontend/tailwind.config.js` - Tailwind CSS customization
- `frontend/eslint.config.js` - ESLint rules
- `Dockerfile` - Multi-stage Docker image with hardware acceleration support
- `.dockerignore` - Docker build exclusions

## Platform Requirements

**Development:**
- .NET 8 SDK or later
- Node.js 20+ and npm
- Git for version control
- FFmpeg (for video transcoding features)

**Production:**
- Docker (multi-platform: linux/amd64, linux/arm64)
- Alternatively: .NET 8 runtime on Windows/Linux/macOS
- SQLite 3 (included via SQLitePCLRaw)
- FFmpeg with hardware acceleration drivers (VAAPI for all architectures, Intel QSV for amd64, NVIDIA NVENC via host runtime)

**Hardware Acceleration (Docker):**
- Intel Quick Sync Video (QSV) - libmfx-gen1.2, libmfx1, libvpl2 (amd64 only)
- AMD/Intel VAAPI - libva2, libva-drm2, va-driver-all, mesa-va-drivers (all architectures)
- NVIDIA NVENC - Requires NVIDIA runtime (docker runtime configured by user)

---

*Stack analysis: 2026-06-01*
