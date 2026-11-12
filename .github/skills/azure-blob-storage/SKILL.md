---
name: azure-blob-storage
description: 'Azure Storage SDK patterns. Blob upload/download, SAS token generation with expiry, container naming convention per module, MIME type validation, file size limits, WebTWAIN scanner document capture integration.'
---

# Azure Blob Storage Skill

## Container Naming Convention

```
{module}-{purpose}
```

Examples:
- `patients-documents`
- `referrals-attachments`
- `billing-invoices`

## Blob Upload

```csharp
public async Task<Result<string>> UploadAsync(Stream stream, string fileName, string contentType, string module)
{
    // Validate MIME type
    if (!_allowedMimeTypes.Contains(contentType))
        return Result.Failure($"Unsupported file type: {contentType}");

    // Validate file size (max 50MB)
    if (stream.Length > 50 * 1024 * 1024)
        return Result.Failure("File exceeds 50MB limit");

    var containerName = $"{module}-documents";
    var blobName = $"{Guid.NewGuid()}/{fileName}";

    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    await containerClient.CreateIfNotExistsAsync();

    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

    return Result.Success(blobClient.Uri.ToString());
}
```

## SAS Token Generation

```csharp
public string GenerateSasUrl(string blobUri, TimeSpan expiry)
{
    var blobClient = new BlobClient(new Uri(blobUri), _credential);
    var sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = blobClient.BlobContainerName,
        BlobName = blobClient.Name,
        Resource = "b",
        ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
    };
    sasBuilder.SetPermissions(BlobSasPermissions.Read);

    return blobClient.GenerateSasUri(sasBuilder).ToString();
}
```

## Allowed MIME Types

```csharp
private static readonly HashSet<string> _allowedMimeTypes = new()
{
    "application/pdf",
    "image/png",
    "image/jpeg",
    "image/tiff",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
};
```

## WebTWAIN Scanner Integration

- Accept TIFF output from WebTWAIN SDK.
- Convert multi-page TIFF to PDF if needed.
- Store with metadata: `scannedAt`, `scannedBy`, `pageCount`.

## Rules

- Always validate MIME type before upload.
- Enforce file size limits (default 50MB).
- SAS tokens: max 1 hour expiry for downloads, 5 minutes for uploads.
- Container names: lowercase, hyphens only, module-scoped.
- Never serve blobs publicly — always use SAS tokens.
