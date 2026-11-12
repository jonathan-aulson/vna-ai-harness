---
name: logging
description: 'Structured logging with ILogger. Log levels (Debug/Info/Warning/Error), structured property syntax, correlation ID propagation, Application Insights custom events, never log PII — no passwords, tokens, card numbers, SSNs, or DOBs.'
---

# Logging Skill

## Log Levels

| Level | Use |
|-------|-----|
| Debug | Development-time detail, disabled in production |
| Information | Business-meaningful events (patient created, payment processed) |
| Warning | Recoverable issues (retry triggered, cache miss) |
| Error | Unrecoverable failures requiring attention |

## Structured Properties

```csharp
_logger.LogInformation("Patient {PatientId} created in module {Module}", patient.Id, "Patients");
_logger.LogError(ex, "Payment failed for {PaymentIntentId} with code {ErrorCode}", intentId, code);
```

- Use `{PropertyName}` syntax — never string interpolation.
- PascalCase property names.
- Include identifiers that aid debugging.

## Correlation ID Propagation

```csharp
using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["UserId"] = userId
});
```

## Application Insights Custom Events

```csharp
_telemetryClient.TrackEvent("ReferralSubmitted", new Dictionary<string, string>
{
    ["ReferralId"] = referralId.ToString(),
    ["Provider"] = providerName
});
```

## PII Rules — NEVER LOG:

- Passwords or authentication tokens
- Credit card numbers (even partial)
- Social Security Numbers (SSNs)
- Dates of birth (DOBs)
- Patient health information (PHI)
- API keys or secrets

## Anti-Patterns

```csharp
// BAD: String interpolation
_logger.LogInformation($"Patient {patient.Name} created"); // No structured properties

// BAD: PII exposure
_logger.LogInformation("User logged in with SSN {SSN}", user.Ssn);

// BAD: Wrong level
_logger.LogError("Cache miss for key {Key}", key); // Should be Warning
```
