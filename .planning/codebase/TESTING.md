# Testing Patterns

**Analysis Date:** 2026-06-01

## Test Framework

**Runner:**
- C# Backend: xUnit 2.6.2
  - Config: `tests/Sportarr.Api.Tests/Sportarr.Api.Tests.csproj`
  - Microsoft.NET.Test.Sdk 17.8.0
  - xunit.runner.visualstudio 2.5.4 (Visual Studio integration)

- TypeScript/React Frontend: Vitest 4.0.14
  - Config: `frontend/vitest.config.ts`
  - Environment: jsdom (browser-like testing)
  - Setup files: `src/test/setup.ts`

**Assertion Library:**
- C# Backend: FluentAssertions 6.12.0
  - Fluent, readable assertion syntax: `result.Should().Be("expected")`
  - Built-in matchers for collections, strings, dates, exceptions

- TypeScript/React Frontend: Vitest built-in with Testing Library
  - `@testing-library/react` 15.0.0 for component assertions
  - `@testing-library/jest-dom` 6.1.5 for DOM matchers

**Run Commands:**
```bash
# C# Backend
dotnet test tests/Sportarr.Api.Tests/Sportarr.Api.Tests.csproj

# TypeScript Frontend
npm test                     # Run all tests
npm run test:ui            # Watch mode with UI
npm run test:coverage      # Generate coverage report
```

## Test File Organization

**C# Backend:**

**Location:**
- `tests/Sportarr.Api.Tests/` - Single test project
- Test files organized by feature under `Services/` subdirectory
- File naming: `[Subject]Tests.cs` (e.g., `FileNamingServiceTests.cs`, `ReleaseMatchScorerTests.cs`)
- Files mirror the service/component they test

**Naming Convention:**
- Test classes: `[ClassName]Tests` inheriting from xUnit (no base class needed)
- Test methods: `[MethodName]_[Scenario]_[ExpectedResult]` using Pascal case
  - Example: `BuildFileName_ShouldReplaceBasicTokens()`
  - Example: `EvaluateRelease_ShouldDetectQualityAndScore()`
  - Example: `NhlPlayoffGame_WithBroadcastDateSet_ScoresAboveMinimum()`

**TypeScript Frontend:**

**Location:**
- Tests co-located in `__tests__` subdirectories next to the code they test
- Path: `src/pages/__tests__/[Component].test.tsx`
- Path: `src/hooks/__tests__/[Hook].test.ts`
- Path: `src/components/__tests__/[Component].test.tsx`
- File naming: `[Subject].test.tsx` or `[Subject].test.ts`

**Naming Convention:**
- Test suites: `describe('ComponentName', () => { ... })`
- Test cases: `it('should [description]', () => { ... })`
  - Example: `it('should render events page', () => { ... })`
  - Example: `it('should search events when typing in search box', async () => { ... })`
  - Example: `it('should not search with less than 3 characters', async () => { ... })`

## Test Structure

**C# Test Suite Organization:**

```csharp
using Sportarr.Api.Services;
using Sportarr.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sportarr.Api.Tests.Services;

public class FileNamingServiceTests
{
    private readonly FileNamingService _service;
    private readonly Mock<ILogger<FileNamingService>> _mockLogger;

    public FileNamingServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileNamingService>>();
        _service = new FileNamingService(_mockLogger.Object);
    }

    [Fact]
    public void BuildFileName_ShouldReplaceBasicTokens()
    {
        // Arrange
        var format = "{Event Title} - {Quality}";
        var tokens = new FileNamingTokens { EventTitle = "UFC 300", Quality = "1080p" };

        // Act
        var result = _service.BuildFileName(format, tokens, ".mkv");

        // Assert
        result.Should().Be("UFC 300 - 1080p.mkv");
    }
}
```

**Patterns:**
- **AAA Pattern:** Arrange, Act, Assert
  - Arrange: Set up test data and mocks
  - Act: Call the method under test
  - Assert: Verify the result using FluentAssertions
  
- **Constructor Setup:** Mock dependencies in constructor, initialize service under test
  - One-time setup per test class
  - Fresh mocks/instances for each test via xUnit isolation

- **Attributes:**
  - `[Fact]` - Single, fixed test case
  - `[Theory]` + `[InlineData(...)]` - Parameterized tests with multiple input sets
    - Example: `[InlineData("UFC.300.2024.2160p.WEB-DL.x265", "WEBDL-2160p", 630)]`

**TypeScript Test Suite Organization:**

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, userEvent } from '../../test/test-utils';
import EventsPage from '../EventsPage';
import apiClient from '../../api/client';

vi.mock('../../api/client');

describe('EventsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render events page', () => {
    renderWithProviders(<EventsPage />);
    expect(screen.getByText('UFC 300')).toBeInTheDocument();
  });

  it('should search events when typing in search box', async () => {
    const user = userEvent.setup();
    renderWithProviders(<EventsPage />);
    
    const searchInput = screen.getByPlaceholderText(/search for events/i);
    await user.type(searchInput, 'UFC 301');

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith(
        '/search/events',
        expect.objectContaining({ params: { q: 'UFC 301' } })
      );
    }, { timeout: 1000 });
  });
});
```

**Patterns:**
- **Setup & Teardown:**
  - `beforeEach()` - Runs before each test (clear mocks, reset state)
  - No `afterEach()` needed; Testing Library cleanup handles DOM cleanup automatically via setup.ts

- **Mocking:**
  - `vi.mock('...')` - Module-level mocking at top of file
  - `vi.spyOn()` - Spy on console or function calls
  - `vi.clearAllMocks()` - Reset all mocks between tests
  - Returns from mocks: `vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [] })`

- **User Interaction:**
  - `userEvent.setup()` - Creates user instance for realistic interactions
  - `await user.type(element, text)` - Type text into input
  - `await user.click(element)` - Click element
  - `await user.clear(element)` - Clear input value

- **Assertions:**
  - `expect(screen.getByText('text')).toBeInTheDocument()` - DOM matchers from jest-dom
  - `expect(apiClient.get).toHaveBeenCalledWith(...)` - Mock call verification
  - `expect(apiClient.get).not.toHaveBeenCalled()` - Negative assertions
  - `expect(apiClient.get).toHaveBeenCalledTimes(1)` - Call count verification

- **Async Handling:**
  - `await waitFor(() => { ... }, { timeout: 1000 })` - Wait for async operations
  - `async () => { ... }` for test functions with async operations
  - Debounce waits in tests: `await waitFor(() => {}, { timeout: 600 })`

## Mocking

**Framework:** Moq (C#), Vitest (TypeScript)

**C# Mocking Patterns:**

```csharp
private readonly Mock<ILogger<FileNamingService>> _mockLogger;

public FileNamingServiceTests()
{
    _mockLogger = new Mock<ILogger<FileNamingService>>();
    _service = new FileNamingService(_mockLogger.Object);
}

// Moq creates mock objects with Mock.Of<T>()
// Verify calls: _mockLogger.Verify(...)
```

**What to Mock:**
- External dependencies: Loggers, HTTP clients, database contexts (use in-memory for EF)
- Services injected via constructor
- Database accessed via in-memory provider (Microsoft.EntityFrameworkCore.InMemory)

**What NOT to Mock:**
- The class under test itself
- Data models (use real instances with test data)
- Helper methods (test them directly)

**TypeScript Mocking Patterns:**

```typescript
// Module-level mocking
vi.mock('../../api/client');

// Function-level spy
const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

// Mock return value
vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockSearchResults });

// Cleanup after test
consoleSpy.mockRestore();
```

**What to Mock:**
- API calls: Use `vi.mock('../../api/client')` to replace with test doubles
- Hooks: Module-level mocking at top of test file for consistency
- console methods: When testing error handling, spy on console.error
- Browser APIs: window.matchMedia, IntersectionObserver (set up in setup.ts)

**What NOT to Mock:**
- React Router: Use real `BrowserRouter` in test utils
- Query providers: Use real `QueryClientProvider` (but with test QueryClient)
- Component tree under test: Only mock external APIs, not components

## Fixtures and Factories

**C# Test Data:**
- Inline object creation with named parameters in tests
- Example from `ReleaseMatchScorerTests.cs`:
  ```csharp
  var evt = new Event
  {
      Id = 1,
      Title = "Anaheim Ducks vs Edmonton Oilers",
      Sport = "Ice Hockey",
      EventDate = new DateTime(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc),
      BroadcastDate = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
      HomeTeamName = "Anaheim Ducks",
      AwayTeamName = "Edmonton Oilers",
      Round = "125",
      League = new League { Id = 1, Name = "NHL", Sport = "Ice Hockey" }
  };
  ```

**TypeScript Test Data:**
- Inline object literals in test setup
- Example from `EventsPage.test.tsx`:
  ```typescript
  const mockSearchResults = [
    {
      tapologyId: 'search-1',
      title: 'UFC 301',
      organization: 'UFC',
      eventDate: '2024-06-01',
    },
  ];
  ```

**Location:**
- No centralized fixture files observed
- Fixtures defined locally in each test file or at module level
- Consider adding `fixtures/` or `mockData/` directory if test data becomes large

**Test Utilities:**
- `frontend/src/test/test-utils.tsx` - Custom render function with providers
  ```typescript
  export function renderWithProviders(
    ui: ReactElement,
    options?: Omit<RenderOptions, 'wrapper'>,
  ) {
    function Wrapper({ children }: { children: React.ReactNode }) {
      return (
        <QueryClientProvider client={testQueryClient}>
          <BrowserRouter>
            {children}
          </BrowserRouter>
        </QueryClientProvider>
      );
    }
    return render(ui, { wrapper: Wrapper, ...options });
  }
  ```
  - Re-exports from Testing Library: `screen`, `userEvent`
  - Provides test QueryClient with disabled retries

## Coverage

**Requirements:** Not enforced (no minimum coverage threshold found in config)

**View Coverage:**
```bash
npm run test:coverage        # Frontend coverage report
```

Coverage output:
- Provider: v8 (Vitest default)
- Reporters: text, json, html
- Excluded: node_modules, test utilities, config files, dist

Coverage is optional; developers can review but CI doesn't enforce minimum.

## Test Types

**Unit Tests:**

**Scope:** Single method/function with mocked dependencies

**Approach:**
- C# Services: Test public methods in isolation
  - Example: `FileNamingServiceTests` tests `BuildFileName()`, `BuildFolderPath()` methods
  - Mocks logger but not data structures
  - Uses FluentAssertions for readable assertions
  - Parameterized tests via `[Theory]` + `[InlineData]` for multiple scenarios

- TypeScript Components: Test rendering and user interactions
  - Example: `EventsPage.test.tsx` tests component render, search input, selection
  - Mocks API client but renders real component
  - Uses `@testing-library/react` for user-centric queries (getByText, getByPlaceholderText)
  - No snapshot testing observed

**Integration Tests:**
- Observed in C# with in-memory database
- `Microsoft.EntityFrameworkCore.InMemory` for database testing without external dependencies
- Not heavily used; focus is on unit tests

**E2E Tests:**
- Not observed in codebase
- No Cypress, Playwright, or similar tool configured

## Common Patterns

**Async Testing:**

C#:
```csharp
public async Task GetEvent_ShouldFetchFromDatabase()
{
    // Arrange
    var eventId = 1;
    
    // Act
    var result = await _service.GetEventAsync(eventId);
    
    // Assert
    result.Should().NotBeNull();
}
```

TypeScript:
```typescript
it('should search events when typing in search box', async () => {
    const user = userEvent.setup();
    renderWithProviders(<EventsPage />);

    const searchInput = screen.getByPlaceholderText(/search for events/i);
    await user.type(searchInput, 'UFC 301');

    await waitFor(() => {
        expect(apiClient.get).toHaveBeenCalledWith('/search/events', ...);
    }, { timeout: 1000 });
});
```

**Error Testing:**

C#:
```csharp
[Fact]
public void CreateEvent_WithInvalidTitle_ShouldThrowValidationException()
{
    // Arrange
    var invalidRequest = new CreateEventRequest { Title = "" };

    // Act
    var action = () => _validator.Validate(invalidRequest);

    // Assert
    action.Should().Throw<ValidationException>();
}
```

TypeScript:
```typescript
it('should handle API search errors gracefully', async () => {
    const user = userEvent.setup();
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Search failed'));

    renderWithProviders(<EventsPage />);
    const searchInput = screen.getByPlaceholderText(/search for events/i);
    await user.type(searchInput, 'UFC 301');

    await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalled();
    });

    consoleSpy.mockRestore();
});
```

## Test Documentation Patterns

**Comment Style:**
- C# tests use comments for complex scenarios and regression cases
- Example from `ReleaseMatchScorerTests.cs`:
  ```csharp
  /// <summary>
  /// User-reported case: Anaheim Ducks vs Edmonton Oilers, NHL Stanley Cup
  /// Round 1 Game 6, played Apr 30 in venue-local (ET) but stored on May 1
  /// for a UK-timezone user. The release is correctly named with venue-local
  /// date "30.04.2026" and Round 1 / Game 6 markers. Both teams are in the
  /// release title. This MUST score above MinimumMatchScore.
  /// </summary>
  [Fact]
  public void NhlPlayoffGame_WithBroadcastDateSet_ScoresAboveMinimum()
  ```

- TypeScript tests use `it()` descriptions for clarity; skip complex scenarios with `it.skip()`

## Known Testing Gaps

**Coverage Observations:**
- E2E tests not present; manual testing likely required for full user workflows
- Some TypeScript tests marked `it.skip()` with comments about complexity
  - Example: Loading state tests skipped due to module-level mocking complexity
  - Example: Empty list tests skipped, manual testing confirms correctness

---

*Testing analysis: 2026-06-01*
