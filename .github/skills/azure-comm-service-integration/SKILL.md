---
name: azure-comm-service-integration
description: 'Azure Communication Services SDK patterns. Email sending with templates, video call token generation, Teams room creation, delivery receipt webhook handling, opt-out compliance.'
---

# Azure Communication Services Integration Skill

## Email Sending with Templates

```csharp
public async Task<Result> SendEmailAsync(string recipientEmail, string templateName, Dictionary<string, string> parameters)
{
    var content = await _templateService.RenderAsync(templateName, parameters);

    var emailMessage = new EmailMessage(
        senderAddress: _config.SenderAddress, // e.g., "DoNotReply@{domain}.azurecomm.net"
        recipientAddress: recipientEmail,
        content: new EmailContent(content.Subject) { Html = content.HtmlBody });

    var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

    return operation.Value.Status == EmailSendStatus.Succeeded
        ? Result.Success()
        : Result.Failure($"Email send failed: {operation.Value.Status}");
}
```

## Video Call Token Generation

```csharp
public async Task<CallToken> GenerateCallTokenAsync(string userId)
{
    var identityClient = new CommunicationIdentityClient(_connectionString);
    var user = new CommunicationUserIdentifier(userId);

    var tokenResponse = await identityClient.GetTokenAsync(
        user,
        scopes: new[] { CommunicationTokenScope.VoIP });

    return new CallToken(tokenResponse.Value.Token, tokenResponse.Value.ExpiresOn);
}
```

## Teams Room Creation

```csharp
public async Task<RoomInfo> CreateRoomAsync(DateTimeOffset scheduledStart, TimeSpan duration, IEnumerable<string> participantIds)
{
    var roomsClient = new RoomsClient(_connectionString);

    var participants = participantIds.Select(id =>
        new RoomParticipant(new CommunicationUserIdentifier(id)) { Role = ParticipantRole.Attendee });

    var room = await roomsClient.CreateRoomAsync(
        validFrom: scheduledStart,
        validUntil: scheduledStart.Add(duration),
        participants: participants);

    return new RoomInfo(room.Value.Id, room.Value.CreatedAt);
}
```

## Delivery Receipt Webhook

```csharp
public async Task<IResult> HandleDeliveryReceipt(EventGridEvent[] events)
{
    foreach (var evt in events)
    {
        if (evt.EventType == "Microsoft.Communication.EmailDeliveryReportReceived")
        {
            var data = evt.Data.ToObjectFromJson<EmailDeliveryReportData>();
            await _notificationRepo.UpdateStatusAsync(data.MessageId, data.Status);
        }
    }
    return Results.Ok();
}
```

## Rules

- Always handle opt-out/unsubscribe requests.
- Never send emails to addresses that have bounced or opted out.
- Token expiry: video call tokens valid for 24 hours max.
- Use Event Grid for delivery receipts — don't poll.
- Rate limits: respect Azure Communication Services throttling.
