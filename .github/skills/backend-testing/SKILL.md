---
name: backend-testing
description: 'xUnit AAA pattern, service unit tests with mocked repositories, integration tests using TestContainers for SQL Server, test naming convention: Method_Scenario_ExpectedResult.'
---

# Backend Testing Skill

## Naming Convention

```
Method_Scenario_ExpectedResult
```

Examples:
- `CreateAsync_ValidRequest_ReturnsSuccessWithId`
- `CreateAsync_DuplicateEmail_ReturnsConflictError`
- `GetByIdAsync_NotFound_ReturnsFailureResult`

## AAA Pattern

```csharp
[Fact]
public async Task CreateAsync_ValidRequest_ReturnsSuccessWithId()
{
    // Arrange
    var request = new CreateReferralRequest { PatientName = "John Doe" };
    var service = new CreateReferralService(_mockRepository.Object, _mockValidator.Object, _mockDispatcher.Object);

    // Act
    var result = await service.CreateAsync(request);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.Id.Should().NotBeEmpty();
}
```

## Mocking Repositories

```csharp
var mockRepository = new Mock<CreateReferralRepository>();
mockRepository
    .Setup(r => r.SaveAsync(It.IsAny<ReferralModel>()))
    .Returns(Task.CompletedTask);
```

Mock the feature repository directly — do not mock `ApplicationDbContext` in unit tests.

## Integration Tests with TestContainers

```csharp
public class ReferralIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        // Apply migrations to container
    }

    public async Task DisposeAsync() => await _sqlContainer.DisposeAsync();

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        // Use _sqlContainer.GetConnectionString() for real DB tests
    }
}
```

## Coverage Thresholds

- Services: 90%+ branch coverage
- Validators: 100% rule coverage
- Controllers: Integration test for happy path + primary error path
