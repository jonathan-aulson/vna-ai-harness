---
description: "Use when writing tests, setting up test infrastructure, or discussing test strategy. Covers backend testing (xUnit, NSubstitute, TestContainers) and frontend testing (Vitest, RTL, Playwright)."
---
# Testing Standards

## Test Naming Convention

All tests (backend and frontend) follow this pattern:

```
MethodOrScenario_ShouldExpectedBehavior
```

Examples:
- `CreateReferral_ShouldReturnSuccess_WhenValidInput`
- `CreateReferral_ShouldReturnValidationError_WhenMissingRequiredFields`
- `useReferralList_ShouldFetchAndCacheResults`

---

## Backend Testing

### Stack

- **xUnit** — test framework
- **NSubstitute** — mocking (NOT Moq)
- **Respawn** — database reset between integration tests
- **TestContainers** — real SQL Server in Docker for integration tests

### Unit Tests

- AAA pattern (Arrange, Act, Assert)
- Test use-case/handler classes directly with mocked dependencies
- Validators tested in isolation with sample inputs

```csharp
[Fact]
public async Task CreateReferral_ShouldReturnSuccess_WhenValidInput()
{
    // Arrange
    var repository = Substitute.For<IReferralRepository>();
    var handler = new CreateReferralHandler(repository);
    var command = new CreateReferralCommand { /* ... */ };

    // Act
    var result = await handler.HandleAsync(command);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Integration Tests

- Use TestContainers for a real SQL Server instance
- Use Respawn to reset DB state between tests
- Test full request pipeline (controller → handler → DB)
- Do NOT mock the database in integration tests

### What NOT to Test

- Do NOT test private methods directly
- Do NOT test EF-generated code
- Do NOT write tests for trivial getters/setters

---

## Frontend Testing

### Stack

- **Vitest** — test framework (NOT Jest)
- **React Testing Library (RTL)** — component tests
- **Playwright** — E2E browser tests

### Component Tests

- Render with necessary providers (QueryClient, Router, etc.)
- Query elements using accessibility-first selectors (`getByRole`, `getByLabelText`)
- Use `waitFor` for async assertions
- Mock the generated API client, not `fetch` directly

```typescript
it('CreateReferralForm_ShouldShowValidationErrors_WhenSubmittedEmpty', async () => {
  render(<CreateReferralForm />, { wrapper: TestProviders });

  await userEvent.click(screen.getByRole('button', { name: /submit/i }));

  await waitFor(() => {
    expect(screen.getByText(/required/i)).toBeInTheDocument();
  });
});
```

### E2E Tests (Playwright)

- Test critical user flows end-to-end
- Use page objects for maintainability
- Run against a real (or Docker-composed) backend
- Do NOT use E2E tests for unit-level validation logic
