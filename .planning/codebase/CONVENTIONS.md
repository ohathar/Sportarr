# Coding Conventions

**Analysis Date:** 2026-06-01

## Naming Patterns

**Files:**
- C# files: PascalCase matching the class/namespace they contain. Examples:
  - `FileNamingService.cs` - service class
  - `ExceptionHandlingMiddleware.cs` - middleware class
  - `ReleaseMatchScorerTests.cs` - test class
  - Validators follow pattern: `[Subject]Validator.cs` (e.g., `CreateEventRequestValidator.cs`)
  - Endpoints follow pattern: `[Feature]Endpoints.cs` (e.g., `EventEndpoints.cs`, `SonarrConfigEndpoints.cs`)

- TypeScript/React files: PascalCase for components/pages, camelCase for utilities
  - Components: `EventsPage.tsx`, `AddEventModal.tsx`, `ErrorBoundary.tsx`
  - Pages: `EventsPage.tsx`, `LeaguesPage.tsx`, `SystemHealthPage.tsx`
  - Tests: `__tests__/[Component].test.tsx` (underscore-wrapped `__tests__` directory)
  - Utilities/Hooks: `useSettings.ts`, `test-utils.tsx`

**Classes & Types:**
- C# classes: PascalCase (e.g., `FileNamingService`, `Organization`, `Event`, `ExceptionHandlingMiddleware`)
- Interfaces: PrefixI + PascalCase (not used extensively; framework uses concrete classes)
- Exceptions: PascalCase ending in `Exception` (e.g., `FileImportException`, `ValidationException`)
- React components: PascalCase (e.g., `EventsPage`, `AddEventModal`, `ErrorBoundary`)

**Properties & Fields:**
- C# public properties: PascalCase with get/set (e.g., `Id`, `Title`, `Sport`, `EventDate`, `HomeTeamName`)
- C# private fields: underscore prefix + camelCase (e.g., `_logger`, `_next`, `_environment`)
- C# static readonly fields: PascalCase (e.g., `InvalidFileChars`, `SkipPathPrefixes`, `QuietPollPathPrefixes`)
- TypeScript properties: camelCase (e.g., `id`, `title`, `eventDate`)

**Methods & Functions:**
- C# public methods: PascalCase (e.g., `BuildFileName()`, `BuildFolderPath()`, `CalculateMatchScore()`)
- C# private methods: PascalCase (e.g., `GetFolderTokens()`, `ReplaceTokens()`, `CleanTitle()`)
- C# async methods: Include `Async` suffix (e.g., `InvokeAsync()`, `HandleExceptionAsync()`)
- TypeScript functions: camelCase (e.g., `renderWithProviders()`, `getTodayDateString()`)
- React hooks: Prefix `use` + PascalCase (e.g., `useSettings()`, `useEvents()`, `useInertCleanup()`)

**Variables & Constants:**
- C# local variables: camelCase (e.g., `folderName`, `pathParts`, `leagueFolder`)
- C# constants (local): camelCase with `const` keyword (e.g., `const string EventFolderFormat = ...`)
- TypeScript variables: camelCase (e.g., `searchQuery`, `selectedSport`, `isModalOpen`)
- TypeScript constants: UPPER_SNAKE_CASE (e.g., `SPORT_FILTERS`, `InvalidFileChars`)

**Namespaces:**
- C# namespaces follow directory structure: `Sportarr.Api.Services`, `Sportarr.Api.Endpoints`, `Sportarr.Api.Middleware`, `Sportarr.Api.Models`, `Sportarr.Api.Validators`
- File-scoped namespaces used (modern C# style): `namespace Sportarr.Api.Services;`

## Code Style

**Formatting:**
- Language: C# (backend), TypeScript/React (frontend)
- C# Version: net8.0 with nullable reference types enabled (`<Nullable>enable</Nullable>`)
- TypeScript Version: ~5.9.3
- Line length: No explicit limit observed, but code keeps lines readable
- Indentation: 4 spaces (C#), 2 spaces (TypeScript/JSX inferred from package.json)

**Linting:**
- C# Linter: Built-in .NET analyzers (nullable reference type checks, implicit usings)
  - Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
  - Nullable reference types enabled for type safety
  
- TypeScript/React Linter: ESLint with typescript-eslint
  - Config: `frontend/eslint.config.js`
  - Rules: Recommended configs for JS, TypeScript, React hooks, React Refresh
  - No strict rules found; uses baseline recommendations
  
- Code Formatter: Prettier (expected but no .prettierrc found; follows default Prettier style in frontend)

## Import Organization

**C# Imports:**
1. System namespaces (`using System;`, `using System.Linq;`)
2. External packages (`using Microsoft.EntityFrameworkCore;`, `using FluentValidation;`)
3. Project namespaces (`using Sportarr.Api.Models;`, `using Sportarr.Api.Services;`)
4. File-scoped namespace declaration at end

Example from `Program.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Sportarr.Api.Data;
using Sportarr.Api.Endpoints;
using Sportarr.Api.Models;
// ... more imports

// Entry point code
```

**TypeScript/React Imports:**
1. React/library imports (e.g., `import { useState } from 'react'`)
2. Third-party packages (e.g., `import { QueryClient, QueryClientProvider } from '@tanstack/react-query'`)
3. Local relative imports
4. CSS/style imports at end

Example from `App.tsx`:
```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import Layout from './components/Layout';
import { ErrorBoundary } from './components/ErrorBoundary';
```

**Path Aliases:**
- TypeScript: `@` alias to `src/` directory (configured in `frontend/vitest.config.ts` and `vite.config.ts`)
- C# implicit usings eliminate need for aliases

## Error Handling

**C# Exception Strategy:**
- Custom exception hierarchy rooted in `SportarrException` base class (`src/Exceptions/SportarrExceptions.cs`)
- Specific exceptions for different domains:
  - `FileImportException` - File operations with optional FilePath and EventId context
  - `DownloadClientException` - Download client failures with ClientName and ClientType
  - `IndexerException` - Indexer operations with IndexerName, SearchQuery, HttpStatusCode
  - `TaskExecutionException` - Background task failures with TaskName and TaskId
  - `ConfigurationException` - Configuration errors with SettingName context
  - `ValidationException` - API validation failures with FieldName and ProvidedValue
  - `AuthenticationException` - Auth failures with FailureReason
  - `ExternalApiException` - External API failures with ApiName, Endpoint, HttpStatusCode

- Global exception middleware at `src/Middleware/ExceptionHandlingMiddleware.cs`:
  - Catches all unhandled exceptions
  - Serializes to standardized `ErrorResponse` with HTTP status code, error type, and message
  - Logs with severity appropriate to HTTP status (500+ = Error, 400-499 = Warning, 2xx = Info/Debug)
  - Includes request path and timestamp in response
  - Includes stack traces in development only
  - Sanitizes log inputs to prevent log injection attacks (CWE-117)

**TypeScript/React Error Handling:**
- No global error handler visible in reviewed code, relying on try-catch and React error boundaries
- `ErrorBoundary` component at `src/components/ErrorBoundary.tsx` for React component tree errors
- API errors caught in hooks with `vi.spyOn(console, 'error')` mocking in tests
- Graceful degradation expected in UI (e.g., displaying error messages via toast notifications from Sonner library)

**Validation:**
- C# uses FluentValidation library for declarative validation
- Validators inherit from `AbstractValidator<T>` (e.g., `CreateEventRequestValidator`)
- Validators registered in dependency injection and applied via middleware or endpoints
- Example from `CreateEventRequestValidator`:
  ```csharp
  RuleFor(x => x.Title)
      .NotEmpty().WithMessage("Event title is required.")
      .MaximumLength(500);
  ```

## Logging

**Framework:** Serilog for C#, console for TypeScript/React

**C# Logging:**
- Serilog configured in `Program.cs` with file and console sinks
- Serilog.AspNetCore for structured HTTP request/response logging
- Injected via `ILogger<T>` parameter in constructors
- Structured logging with named parameters: `_logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message)`
- Log levels: Information (default), Debug (detailed/polling), Warning (client errors), Error (server errors)

**Patterns:**
- Request logging middleware at `src/Middleware/RequestLoggingMiddleware.cs`:
  - Logs HTTP method, path, status code, elapsed time
  - Skips noisy paths (health checks, static assets, frontend assets)
  - "Quiet poll" paths (queue, task, search endpoints) log at DEBUG level on success to avoid spam
  - Non-2xx responses on quiet paths still log at appropriate level (WARNING/ERROR)
  
- Exception logging in middleware:
  - Server errors (500+) logged at ERROR level with full context
  - Client errors (400-499) logged at WARNING level
  - Paths and messages sanitized to prevent log injection (CWE-117)
  
- Service logging:
  - Services log errors when operations fail
  - Example: `_logger.LogError(ex, "File import failed for {FilePath}", filePath);`

**TypeScript/React:**
- `console.log()` for debugging (with `[INIT]` prefix pattern for initialization)
- `console.error()` for errors
- No structured logging library; reliance on browser dev tools

## Comments

**When to Comment:**
- Complex business logic that isn't self-documenting (rare in reviewed code)
- Workarounds for known issues (e.g., platform-specific behavior)
- Non-obvious design decisions

**Where Comments Appear:**
- Most comments are inline explanations in code blocks
- Example from `Program.cs`:
  ```csharp
  // Use system SQLite library instead of bundled e_sqlite3 (avoids "invalid opcode" on older CPUs)
  SQLitePCL.Batteries_V2.Init();
  ```
- Example from `RequestLoggingMiddleware.cs`:
  ```csharp
  // Hot-poll endpoints the SPA hits on a 3-second loop (queue widgets,
  // task drawer, activity counters, search-state probes). At INFO
  // they spam the log with 20+ identical 200s per minute and bury
  // the lines that actually matter...
  ```

**XML Documentation Comments (C#):**
- Used extensively in public APIs and classes
- Example from `FileNamingService.cs`:
  ```csharp
  /// <summary>
  /// Build filename from format template and tokens
  /// </summary>
  public string BuildFileName(string format, FileNamingTokens tokens, string extension)
  ```
- Summary tags for method/class purpose
- `<param>` tags for parameters
- `<returns>` tags for return values
- `<exception>` tags for thrown exceptions

**JSDoc/TSDoc:**
- Not observed in reviewed TypeScript files
- Comments are inline and descriptive

## Function Design

**Size:**
- C# methods: Average 20-40 lines, with extraction of helper methods for clarity
- TypeScript functions: Average 10-30 lines with heavy use of React hooks for composition
- No observed methods exceeding 100 lines; complex logic split into multiple methods

**Parameters:**
- C# methods: Dependency injection via constructor, function parameters are data
- Avoid passing too many parameters; use objects/DTOs when needed
- Example: `public string BuildFolderPath(MediaManagementSettings settings, Event eventInfo)`
- TypeScript functions: Parameters typically primitive types or React props

**Return Values:**
- C# methods return strongly-typed objects (strings, DTOs, collections)
- Async methods return `Task<T>` or `Task`
- TypeScript returns match parameter types; React components return JSX.Element
- Null returns used sparingly; `string?`, `int?` for nullable types (C# 8.0+ nullable reference types)

**Example from Codebase:**
```csharp
public string BuildFolderPath(MediaManagementSettings settings, Event eventInfo)
{
    var tokens = GetFolderTokens(eventInfo);
    var pathParts = new List<string>();
    
    // Conditional logic for folder creation
    if (settings.CreateLeagueFolders && !string.IsNullOrWhiteSpace(settings.LeagueFolderFormat))
    {
        var leagueFolder = ReplaceTokens(settings.LeagueFolderFormat, tokens);
        leagueFolder = CleanFileName(leagueFolder);
        if (!string.IsNullOrWhiteSpace(leagueFolder))
        {
            pathParts.Add(leagueFolder);
        }
    }
    
    return string.Join(Path.DirectorySeparatorChar, pathParts);
}
```

## Module Design

**Exports:**
- C#: Public classes and methods are explicitly marked `public`; internal members marked `private`
- No barrel files pattern; each file exports one primary class
- Namespace used for logical grouping and internal access control

- TypeScript: Named exports preferred over default exports
- React components use default exports for page/route components
- Utility functions and hooks use named exports
- Example:
  ```typescript
  export default function EventsPage() { ... }  // Page component
  export function renderWithProviders() { ... }  // Utility
  ```

**Barrel Files:**
- Not observed in reviewed code
- Individual imports from specific files (e.g., `import EventsPage from './pages/EventsPage'`)

**Dependency Injection (C#):**
- Constructor injection pattern throughout
- Services registered in dependency container in `Program.cs`
- Middleware registered in application builder pipeline
- Validators registered with FluentValidation extension

**Example:**
```csharp
public class FileNamingService
{
    private readonly ILogger<FileNamingService> _logger;
    
    public FileNamingService(ILogger<FileNamingService> logger)
    {
        _logger = logger;
    }
}
```

## TypeScript Specific Patterns

**Types:**
- Interfaces used sparingly; concrete types preferred
- Type inference for local variables
- Generic types for collections and promises
- Example:
  ```typescript
  interface TVScheduleEvent {
    idEvent: string;
    strEvent: string;
    strSport: string;
    // ... other properties
  }
  ```

**Async/Await:**
- All async operations use async/await syntax
- Promises avoided in favor of async functions
- Example:
  ```typescript
  async function init() {
    try {
      const response = await fetch(initializeUrl);
      window.Sportarr = await response.json();
    } catch (error) {
      console.error('Failed to initialize Sportarr config:', error);
    }
  }
  ```

---

*Convention analysis: 2026-06-01*
