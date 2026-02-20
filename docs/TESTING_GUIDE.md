# Testing Guide

## Overview

The Favorite Tags plugin includes comprehensive unit, integration, and workflow tests to ensure reliability and correctness.

## Test Structure

```
tests/
├── Configuration/
│   └── ConfigurationStoreTests.cs
├── Models/
│   └── FavoriteItemTests.cs
├── Services/
│   ├── BaseApiServiceTests.cs
│   ├── SonarrServiceTests.cs
│   ├── RadarrServiceTests.cs
│   ├── SyncServiceTests.cs
│   ├── SyncServiceExtendedTests.cs
│   └── SyncHistoryTrackerTests.cs
├── ScheduledTasks/
│   └── FavoritesSyncTaskTests.cs
├── Controllers/
│   └── AdminControllerTests.cs
├── Integration/
│   └── SyncWorkflowTests.cs
└── TestHelpers/
    └── MockDataGenerator.cs
```

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test File
```bash
dotnet test --filter "ClassName=SyncServiceTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~SyncServiceTests.SyncFavoritesAsync_IsIdempotent"
```

### Run with Verbose Output
```bash
dotnet test -v detailed
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Test Categories

### Unit Tests

Individual component tests with mocked dependencies.

**Coverage:**
- Configuration validation
- Data model creation
- Service instantiation
- Error handling

**Key Test Files:**
- `ConfigurationStoreTests.cs` - Configuration loading/saving
- `FavoriteItemTests.cs` - Model creation
- `SonarrServiceTests.cs` - Sonarr service instantiation
- `RadarrServiceTests.cs` - Radarr service instantiation
- `SyncHistoryTrackerTests.cs` - History tracking

### Integration Tests

Multi-component workflows with mocked external services.

**Coverage:**
- Full sync workflows
- Series and movie matching
- Multi-user scenarios
- Error handling across components

**Key Test File:**
- `SyncWorkflowTests.cs` - Complete sync workflows

**Tests include:**
- Series sync by TVDB ID
- Movie sync by IMDB ID
- Mixed series/movie sync
- Partial failures with partial success
- Multi-user favorite aggregation
- Dry-run mode verification
- Configuration validation
- Concurrent sync prevention

### Extended Tests

Comprehensive error scenarios and edge cases.

**Coverage:**
- Error handling
- Configuration combinations
- Idempotency verification
- Partial service failures
- Edge cases

**Key Test File:**
- `SyncServiceExtendedTests.cs` - Error scenarios

**Tests include:**
- Jellyfin unavailable
- Tag creation failure
- Sonarr-only configuration
- Radarr-only configuration
- Already-tagged items (idempotency)
- Items without external IDs
- Partial service failures
- Last sync time tracking
- Disabled plugin behavior

### Controller Tests

REST API endpoint testing.

**Coverage:**
- Configuration GET/POST
- Status retrieval
- Manual sync trigger
- Dry-run sync
- History management
- Connection testing

**Key Test File:**
- `AdminControllerTests.cs` - REST endpoints

## Test Helpers

### MockDataGenerator

Utility class for creating consistent test data.

```csharp
using Jellyfin.Plugin.FavoriteTags.Tests.TestHelpers;

// Create single items
var series = MockDataGenerator.CreateMockSeries();
var movie = MockDataGenerator.CreateMockMovie();

// Create collections
var favorites = MockDataGenerator.CreateMockFavorites(count: 10, mixedTypes: true);

// Create sync results
var successResult = MockDataGenerator.CreateSuccessSyncResult();
var failedResult = MockDataGenerator.CreateFailedSyncResult();

// Create multi-user scenarios
var multiUserFavorites = MockDataGenerator.CreateMockMultiUserFavorites();
```

## Test Coverage Goals

**Target Coverage:**
- Services: >90%
- Sync logic: >95%
- Controllers: >85%
- Models: >80%
- Overall: >85%

## Key Test Scenarios

### Matching

- [x] Match series by TVDB ID (reliable)
- [x] Match series by title (fallback)
- [x] Match movie by IMDB ID (reliable)
- [x] Match movie by title (fallback)
- [x] Unmatched items logged and skipped
- [x] Items without external IDs fallback to title

### Idempotency

- [x] Running sync twice produces same result
- [x] No duplicate tag applications
- [x] Already-tagged items skipped
- [x] Safe to run multiple times

### Multi-user

- [x] Aggregate favorites across all users
- [x] Protection if ANY user favorited
- [x] Unfavorite by one user doesn't remove tag if others have it
- [x] Tag removed only when ALL users unfavorite

### Error Handling

- [x] Jellyfin unavailable fails gracefully
- [x] Sonarr/Radarr unavailable logs error
- [x] Network timeouts retried
- [x] Rate limits handled
- [x] Partial failures don't block other items
- [x] Missing items logged, sync continues

### Configuration

- [x] Sonarr-only setup (Radarr skipped)
- [x] Radarr-only setup (Sonarr skipped)
- [x] Both configured (both used)
- [x] Invalid config fails validation
- [x] Missing required fields rejected

### Dry-run

- [x] Shows what would happen
- [x] No changes applied
- [x] Can verify matching before enabling

## Writing New Tests

### Test Structure

```csharp
[Fact]
public async Task Sync_WithDescriptiveScenario_ExpectedResult()
{
    // Arrange - Setup mocks and data
    var mockData = MockDataGenerator.CreateMockSeries();
    _serviceMock.Setup(x => x.Method()).ReturnsAsync(mockData);

    var service = new ServiceUnderTest(_serviceMock.Object, ...);

    // Act - Execute the test
    var result = await service.DoSomething();

    // Assert - Verify results
    Assert.NotNull(result);
    Assert.True(result.Success);
    _serviceMock.Verify(x => x.Method(), Times.Once);
}
```

### Naming Convention

- `[Class]_[Scenario]_[ExpectedResult]`
- Example: `Sync_WithValidFavorites_ReturnsSuccess`
- Example: `Sync_WithJellyfinError_ReturnsFailure`

### Mocking Guidelines

1. Use `Mock<T>` from Moq
2. Mock external dependencies
3. Keep mocks focused on test scenario
4. Use `MockDataGenerator` for test data
5. Verify important calls

### Async Tests

All async methods should use:
```csharp
[Fact]
public async Task TestName()
{
    var result = await service.AsyncMethod();
    Assert.NotNull(result);
}
```

## Continuous Integration

Tests run automatically on:
- Pull requests
- Commits to main/develop
- Before release

**CI Pipeline:**
1. Build solution
2. Run all tests
3. Generate coverage report
4. Publish results

## Coverage Reports

Generate coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover /p:ExcludeByFile="**/obj/**"
```

Reports generated in `coverage/` directory.

## Debugging Tests

### Visual Studio
1. Set breakpoint in test
2. Right-click test
3. Select "Debug Test"

### Command Line
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Output
```bash
dotnet test -v diagnostic
```

## Performance Tests

Performance targets:
- Unit test suite: <5 seconds
- Integration tests: <10 seconds
- All tests: <15 seconds

## Known Limitations

1. **No Real API Testing** - All external services mocked
2. **In-Memory History** - History not persisted between tests
3. **Single Thread** - Async tests don't truly test concurrency
4. **Limited Network Scenarios** - No packet loss/latency simulation

## Future Test Improvements

- [ ] Load testing with 1000+ favorites
- [ ] Network chaos testing (intermittent failures)
- [ ] Performance profiling
- [ ] Real Jellyfin/Sonarr/Radarr integration tests
- [ ] End-to-end Docker testing

## Troubleshooting

### Tests Not Running
- Check test project builds: `dotnet build tests/`
- Verify xunit installed: `dotnet add package xunit`
- Check test discovery: `dotnet test --list-tests`

### Tests Failing
- Review test output for assertion errors
- Check mock setup matches test scenario
- Verify test data is correct
- Add debug logging

### Slow Tests
- Check for unnecessary delays/sleeps
- Verify mocks don't make real calls
- Consider parallel test execution

### Coverage Not Generated
- Install coverage tool: `dotnet add package coverlet.collector`
- Use correct coverage parameters
- Check output directory permissions

## Best Practices

1. **Test One Thing** - Each test verifies one behavior
2. **Use Descriptive Names** - Name clearly states what's tested
3. **Arrange-Act-Assert** - Clear test structure
4. **Don't Test Framework** - Test your code, not framework
5. **Mock External Dependencies** - Isolate code under test
6. **Test Edge Cases** - Empty lists, null values, errors
7. **Keep Tests Fast** - No real I/O or waits
8. **Maintain Tests** - Update when code changes
9. **Use Test Helpers** - MockDataGenerator for consistency
10. **Verify Calls** - Use `.Verify()` for important interactions

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)
