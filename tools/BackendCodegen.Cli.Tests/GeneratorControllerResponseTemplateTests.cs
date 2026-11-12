using System.Text.Json;
using BackendCodegen.Cli;
using Xunit;

namespace BackendCodegen.Cli.Tests;

public sealed class GeneratorControllerResponseTemplateTests
{
    [Fact]
    public async Task RunAsync_ShouldEmitMetadataDrivenResponses_WithImplicit404WhenMissing()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var backendRoot = Path.Combine(tempRoot, "backend");
            var appRoot = Path.Combine(backendRoot, "src", "ApiHost", "Features", "Clients");
            var domainRoot = Path.Combine(backendRoot, "src", "Infrastructure", "Database", "Models");
            var infraRoot = Path.Combine(backendRoot, "src", "Infrastructure", "Database");
            var testRoot = Path.Combine(backendRoot, "tests", "Backend.Tests", "Features");
            var featureRoot = appRoot;

            Directory.CreateDirectory(featureRoot);
            Directory.CreateDirectory(testRoot);
            Directory.CreateDirectory(domainRoot);
            Directory.CreateDirectory(infraRoot);

            var contract = BuildBaseContract(
                backendRoot,
                featureRoot,
                appRoot,
                domainRoot,
                infraRoot,
                testRoot,
                responses: [
                    new OperationResponse(200, "GetClientResponse", "Client found", "application/json"),
                    new OperationResponse(400, "ProblemDetails", "Invalid request", "application/problem+json")
                ]);

            var contractPath = Path.Combine(tempRoot, "contract.json");
            await File.WriteAllTextAsync(contractPath, JsonSerializer.Serialize(contract));

            var templatesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BackendCodegen.Cli", "Templates"));
            var generator = new Generator(contract, contractPath, templatesPath);

            var exitCode = await generator.RunAsync();

            Assert.Equal(0, exitCode);

            var controllerPath = Path.Combine(featureRoot, "ClientsController.cs");
            var controller = await File.ReadAllTextAsync(controllerPath);
            Assert.Contains("[ProducesResponseType(typeof(GetClientResponse), 200, \"application/json\", Description = \"Client found\")]", controller);
            Assert.Contains("[ProducesResponseType(typeof(ProblemDetails), 400, \"application/problem+json\", Description = \"Invalid request\")]", controller);
            Assert.Contains("[ProducesResponseType(typeof(ProblemDetails), 404)]", controller);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task RunAsync_ShouldPreserveLegacyFallbackResponses_WhenMetadataMissing()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var backendRoot = Path.Combine(tempRoot, "backend");
            var appRoot = Path.Combine(backendRoot, "src", "ApiHost", "Features", "Clients");
            var domainRoot = Path.Combine(backendRoot, "src", "Infrastructure", "Database", "Models");
            var infraRoot = Path.Combine(backendRoot, "src", "Infrastructure", "Database");
            var testRoot = Path.Combine(backendRoot, "tests", "Backend.Tests", "Features");
            var featureRoot = appRoot;

            Directory.CreateDirectory(featureRoot);
            Directory.CreateDirectory(testRoot);
            Directory.CreateDirectory(domainRoot);
            Directory.CreateDirectory(infraRoot);

            var contract = BuildBaseContract(
                backendRoot,
                featureRoot,
                appRoot,
                domainRoot,
                infraRoot,
                testRoot,
                responses: null);

            var contractPath = Path.Combine(tempRoot, "contract.json");
            await File.WriteAllTextAsync(contractPath, JsonSerializer.Serialize(contract));

            var templatesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BackendCodegen.Cli", "Templates"));
            var generator = new Generator(contract, contractPath, templatesPath);

            var exitCode = await generator.RunAsync();

            Assert.Equal(0, exitCode);

            var controllerPath = Path.Combine(featureRoot, "ClientsController.cs");
            var controller = await File.ReadAllTextAsync(controllerPath);
            Assert.Contains("[ProducesResponseType(typeof(GetClientResponse), 200)]", controller);
            Assert.Contains("[ProducesResponseType(typeof(ProblemDetails), 400)]", controller);
            Assert.Contains("[ProducesResponseType(typeof(ProblemDetails), 404)]", controller);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static BackendGenerationContract BuildBaseContract(
        string backendRoot,
        string featureRoot,
        string appRoot,
        string domainRoot,
        string infraRoot,
        string testRoot,
        List<OperationResponse>? responses)
    {
        return new BackendGenerationContract(
            new WorkItem("8211", "8657", "Swagger completeness", "BE"),
            new Project(
                backendRoot,
                "net10.0",
                Path.Combine(backendRoot, "src", "ApiHost", "ApiHost.csproj"),
                Path.Combine(backendRoot, "tests", "ApiHost.Tests", "ApiHost.Tests.csproj"),
                "ApiHost",
                "Shared.Models",
                "Clients.Application",
                "Clients.Domain.Repositories",
                "Clients.Domain",
                "Clients.Infrastructure",
                appRoot,
                domainRoot,
                infraRoot,
                testRoot,
                "Backend.Tests.Features.Clients"
            ),
            new Feature("Clients", "Client", "api/clients", featureRoot, "Clients", new PrimaryKey("Id", "Guid")),
            new Architecture("FeatureServiceLayer", "ApplicationDbContext", "Result", true, true, true, true),
            [
                new Operation(
                    "Get",
                    "GET",
                    "{id}",
                    null,
                    "GetClientResponse",
                    "GetAsync",
                    ["GetByIdAsync"],
                    200,
                    responses,
                    [1])
            ],
            [new Field("Name", "string", true, 100)],
            new Validation("FluentValidation", []),
            new GenerationFlags(GenerateController: true),
            null,
            new OutputOptions("Direct", true, false),
            null
        );
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "backend-codegen-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }
}
