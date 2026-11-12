---
name: bamboo-health-integration
description: 'Bamboo Health REST API client patterns. Patient DTO mapping, referral submission endpoint, HL7 FHIR resource mapping, Polly retry policy, authentication. Sync only on SUBMITTED status, failed syncs go to outbox.'
---

# Bamboo Health Integration Skill

## Authentication

```csharp
// OAuth2 client credentials flow
var token = await _tokenService.GetTokenAsync("bamboo-health-api");
_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

## Patient DTO Mapping

```csharp
public static BambooPatientDto MapToExternal(Patient patient) => new()
{
    ExternalId = patient.Id.ToString(),
    FirstName = patient.FirstName,
    LastName = patient.LastName,
    DateOfBirth = patient.Dob.ToString("yyyy-MM-dd"),
    Gender = MapGender(patient.Gender),
    // Map to HL7 FHIR Patient resource structure
};
```

## Referral Submission

```csharp
public async Task<Result<string>> SubmitReferral(Referral referral)
{
    // Only sync when status is SUBMITTED
    if (referral.Status != ReferralStatus.Submitted)
        return Result.Failure("Can only sync referrals in Submitted status");

    var dto = MapToExternalReferral(referral);
    var response = await _retryPolicy.ExecuteAsync(() =>
        _httpClient.PostAsJsonAsync("/api/v1/referrals", dto));

    if (!response.IsSuccessStatusCode)
    {
        // Failed syncs go to outbox for retry
        await _outbox.AddAsync(new OutboxMessage("BambooReferral", referral.Id, dto));
        return Result.Failure($"Bamboo Health sync failed: {response.StatusCode}");
    }

    return Result.Success(await response.Content.ReadFromJsonAsync<string>());
}
```

## HL7 FHIR Resource Mapping

Map internal domain models to FHIR R4 resources:
- Patient → FHIR Patient
- Referral → FHIR ServiceRequest
- Provider → FHIR Practitioner

## Polly Retry Policy

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable
                                     || r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

## Rules

- **Only sync on SUBMITTED status** — never sync draft or cancelled referrals.
- **Failed syncs go to outbox** — retry via background worker.
- **Never log PHI** in external API calls — mask patient data in logs.
- **Idempotent submissions** — use referral ID as idempotency key.
