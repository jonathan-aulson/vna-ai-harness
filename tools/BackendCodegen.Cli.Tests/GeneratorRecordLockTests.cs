using BackendCodegen.Cli;
using Xunit;

namespace BackendCodegen.Cli.Tests;

/// <summary>
/// US 11038: every generated entity must carry a record-lock classification
/// (.github/skills/record-locking/SKILL.md).
/// </summary>
public sealed class GeneratorRecordLockTests
{
    private static readonly string TemplatesPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BackendCodegen.Cli", "Templates"));

    [Fact]
    public async Task RunAsync_ShouldEmitLockableRecordAndRowVersion_WhenKindIsRecord()
    {
        using var workspace = new Workspace();

        var exitCode = await workspace.RunAsync(new RecordLockSpec("record"));

        Assert.Equal(0, exitCode);
        var configuration = workspace.ReadConfiguration();
        Assert.Contains("builder.IsLockableRecord();", configuration);
        Assert.Contains("builder.Property(x => x.Version)", configuration);
        Assert.Contains(".IsRowVersion();", configuration);
        Assert.Contains("public byte[] Version { get; set; } = Array.Empty<byte>();", workspace.ReadEntity());
    }

    [Fact]
    public async Task RunAsync_ShouldEmitPartOfRecord_WhenKindIsPartOf()
    {
        using var workspace = new Workspace();

        var exitCode = await workspace.RunAsync(new RecordLockSpec("partOf", Parent: "Client", Fk: "ClientId"));

        Assert.Equal(0, exitCode);
        var configuration = workspace.ReadConfiguration();
        Assert.Contains("builder.IsPartOfRecord<Widget, Client>(x => x.ClientId);", configuration);
        Assert.DoesNotContain("IsRowVersion", configuration);
        Assert.DoesNotContain("Version", workspace.ReadEntity());
    }

    [Fact]
    public async Task RunAsync_ShouldEmitOwnerlessNotLockable_WhenPartOfAllowsNullOwner()
    {
        using var workspace = new Workspace();

        var exitCode = await workspace.RunAsync(
            new RecordLockSpec("partOf", Parent: "Client", Fk: "ClientId", OwnerlessIs: "notLockable"));

        Assert.Equal(0, exitCode);
        Assert.Contains(
            "builder.IsPartOfRecord<Widget, Client>(x => x.ClientId, OwnerlessIs.NotLockable);",
            workspace.ReadConfiguration());
    }

    [Fact]
    public async Task RunAsync_ShouldEmitNotLockableWithEscapedReason_WhenKindIsNotLockable()
    {
        using var workspace = new Workspace();

        var exitCode = await workspace.RunAsync(new RecordLockSpec("notLockable", Reason: "settings: \"widget\" catalog"));

        Assert.Equal(0, exitCode);
        Assert.Contains("builder.IsNotLockable(\"settings: \\\"widget\\\" catalog\");", workspace.ReadConfiguration());
    }

    [Fact]
    public async Task RunAsync_ShouldFailValidation_WhenRecordLockIsMissing()
    {
        using var workspace = new Workspace();

        var (exitCode, errors) = await workspace.RunCapturingErrorsAsync(null);

        Assert.Equal(1, exitCode);
        Assert.Contains("DataModel.RecordLock is required for entity 'Widget'", errors);
        Assert.False(File.Exists(workspace.ConfigurationPath));
        Assert.False(File.Exists(workspace.EntityPath));
    }

    [Theory]
    [InlineData("partOf", null, "ClientId", null, "DataModel.RecordLock.Parent is required")]
    [InlineData("partOf", "Client", null, null, "DataModel.RecordLock.Fk is required")]
    [InlineData("notLockable", null, null, " ", "DataModel.RecordLock.Reason is required")]
    [InlineData("lockable", null, null, null, "DataModel.RecordLock.Kind 'lockable' is not valid")]
    public async Task RunAsync_ShouldFailValidation_WhenRecordLockIsIncomplete(
        string kind, string? parent, string? fk, string? reason, string expectedError)
    {
        using var workspace = new Workspace();

        var (exitCode, errors) = await workspace.RunCapturingErrorsAsync(
            new RecordLockSpec(kind, Parent: parent, Fk: fk, Reason: reason));

        Assert.Equal(1, exitCode);
        Assert.Contains(expectedError, errors);
    }

    [Fact]
    public async Task RunAsync_ShouldNeverEmitSkipRecordLock_OnGeneratedControllers()
    {
        using var workspace = new Workspace();

        var exitCode = await workspace.RunAsync(new RecordLockSpec("record"));

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("SkipRecordLock", await File.ReadAllTextAsync(workspace.ControllerPath));
        Assert.DoesNotContain("SkipRecordLock", await File.ReadAllTextAsync(Path.Combine(TemplatesPath, "Controller.cs.scriban")));
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "backend-codegen-cli-tests", Guid.NewGuid().ToString("N"));
        private readonly string _backendRoot;
        private readonly string _appRoot;
        private readonly string _domainRoot;
        private readonly string _infraRoot;
        private readonly string _testRoot;

        public Workspace()
        {
            _backendRoot = Path.Combine(_root, "backend");
            _appRoot = Path.Combine(_backendRoot, "src", "ApiHost", "Features", "Widgets");
            _domainRoot = Path.Combine(_backendRoot, "src", "Infrastructure", "Database", "Models");
            _infraRoot = Path.Combine(_backendRoot, "src", "Infrastructure", "Database");
            _testRoot = Path.Combine(_backendRoot, "tests", "Backend.Tests", "Features");
            foreach (var directory in new[] { _appRoot, _domainRoot, _infraRoot, _testRoot })
                Directory.CreateDirectory(directory);
        }

        public string EntityPath => Path.Combine(_domainRoot, "Widget.cs");
        public string ConfigurationPath => Path.Combine(_infraRoot, "Configurations", "WidgetConfiguration.cs");
        public string ControllerPath => Path.Combine(_appRoot, "WidgetsController.cs");

        public string ReadEntity() => File.ReadAllText(EntityPath);
        public string ReadConfiguration() => File.ReadAllText(ConfigurationPath);

        public Task<int> RunAsync(RecordLockSpec? recordLock)
        {
            var contract = BuildContract(recordLock);
            return new Generator(contract, Path.Combine(_root, "contract.json"), TemplatesPath).RunAsync();
        }

        public async Task<(int ExitCode, string Errors)> RunCapturingErrorsAsync(RecordLockSpec? recordLock)
        {
            var originalError = Console.Error;
            using var captured = new StringWriter();
            Console.SetError(captured);
            try
            {
                var exitCode = await RunAsync(recordLock);
                return (exitCode, captured.ToString());
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        private BackendGenerationContract BuildContract(RecordLockSpec? recordLock)
            => new(
                new WorkItem("11038", "11038", "Record locking codegen", "DATA"),
                new Project(
                    _backendRoot,
                    "net10.0",
                    Path.Combine(_backendRoot, "src", "ApiHost", "ApiHost.csproj"),
                    Path.Combine(_backendRoot, "tests", "Backend.Tests", "Backend.Tests.csproj"),
                    "ApiHost",
                    "Infrastructure.Shared",
                    "ApiHost.Features.Widgets",
                    "ApiHost.Features.Widgets",
                    "Infrastructure.Database.Models",
                    "Infrastructure.Database",
                    _appRoot,
                    _domainRoot,
                    _infraRoot,
                    _testRoot,
                    "Backend.Tests.Features.Widgets"),
                new Feature("Widgets", "Widget", "api/widgets", _appRoot, "WidgetsModule", new PrimaryKey("WidgetId", "Guid")),
                new Architecture("FeatureServiceLayer", "ApplicationDbContext", "Result", true, true, true, true),
                [
                    new Operation("Get", "GET", "{id}", null, "GetWidgetResponse", "GetAsync", ["GetByIdAsync"], 200, null, [1])
                ],
                [
                    new Field("WidgetId", "Guid", true, 0),
                    new Field("ClientId", "Guid", false, 0),
                    new Field("Name", "string", true, 100)
                ],
                new Validation("FluentValidation", []),
                new GenerationFlags(GenerateController: true, GenerateEntity: true, GenerateEntityConfiguration: true),
                null,
                new OutputOptions("Direct", true, false),
                new DataModel("widget", null, "WidgetId", null, recordLock));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests.
            }
        }
    }
}
