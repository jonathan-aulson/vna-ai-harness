using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;

namespace BackendCodegen.Cli;

public class Generator(BackendGenerationContract contract, string contractPath, string templatesPath)
{
    private readonly List<string> _filesCreated      = [];
    private readonly List<string> _filesSkipped      = [];
    private readonly List<string> _filesModified     = [];
    private readonly List<string> _packagesInstalled = [];
    private readonly List<string> _packagesSkipped   = [];
    private readonly List<string> _warnings          = [];
    private readonly string _contractDir = Path.GetDirectoryName(Path.GetFullPath(contractPath))!;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly string[] ForbiddenPathFragments =
    [
        "/tools/",
        "\\tools\\",
        "/_tmp/",
        "\\_tmp\\",
        "backendcodegen.cli.tests"
    ];

    public async Task<int> RunAsync()
    {
        var contractValidationErrors = ValidateContract();
        if (contractValidationErrors.Count > 0)
        {
            foreach (var error in contractValidationErrors)
                Console.Error.WriteLine($"  ERROR {error}");
            return 1;
        }

        foreach (var pkg in contract.Packages ?? [])
            if (!await InstallPackageAsync(pkg)) return 1;

        var model = BuildModel();

        var f       = contract.Feature;
        var p       = contract.Project;
        var g       = contract.Generation;
        var useSvcIface  = contract.Architecture.UseServiceInterface;
        var useRepoIface = contract.Architecture.UseRepositoryInterface;
        var testRoot     = PathCombineUnix(p.TestRoot, f.Name);
        var entityPath     = PathCombineUnix(p.DomainRoot, $"{f.EntityName}.cs");
        var configPath     = PathCombineUnix(p.InfrastructureRoot, "Configurations", $"{f.EntityName}Configuration.cs");
        var repositoryPath = PathCombineUnix(p.ApplicationRoot, $"{f.EntityName}Repository.cs");

        (string tpl, string output, bool enabled)[] artifacts =
        [
            ("Controller.cs.scriban",                   $"{f.FeatureRoot}/{f.Name}Controller.cs",            g.GenerateController),
            ("IService.cs.scriban",                     $"{f.FeatureRoot}/I{f.EntityName}Service.cs",        g.GenerateService    && useSvcIface),
            ("Service.cs.scriban",                      $"{f.FeatureRoot}/{f.EntityName}Service.cs",          g.GenerateService),
            ("IRepository.cs.scriban",                  $"{f.FeatureRoot}/I{f.EntityName}Repository.cs",     g.GenerateRepository && useRepoIface),
            ("Repository.cs.scriban",                   repositoryPath,                                        g.GenerateRepository),
            ("Validator.cs.scriban",                    $"{f.FeatureRoot}/{f.EntityName}Validator.cs",        g.GenerateValidator),
            ("Dtos.cs.scriban",                         $"{f.FeatureRoot}/Dtos/{f.EntityName}Dtos.cs",        g.GenerateDtos),
            ("Module.cs.scriban",                       $"{f.FeatureRoot}/{f.ModuleName}.cs",                 g.GenerateModule),
            ("README.md.scriban",                       $"{f.FeatureRoot}/README.md",                         g.GenerateReadme),
            ("Tests/ValidatorTests.cs.scriban",         $"{testRoot}/{f.EntityName}ValidatorTests.cs",        g.GenerateTests),
            ("Tests/ServiceTests.cs.scriban",           $"{testRoot}/{f.EntityName}ServiceTests.cs",          g.GenerateTests),
            ("Tests/ControllerTests.cs.scriban",        $"{testRoot}/{f.Name}ControllerTests.cs",             g.GenerateTests),
            ("Data/Entity.cs.scriban",                  entityPath,                                            g.GenerateEntity),
            ("Data/EntityConfiguration.cs.scriban",     configPath,                                            g.GenerateEntityConfiguration),
        ];

        foreach (var (tpl, output, enabled) in artifacts)
        {
            if (!enabled) continue;
            var rendered = await RenderAndWriteAsync(tpl, output, model);
            if (!rendered)
                return 1;
        }

        // Ensure FluentValidation is available in ApiHost (flat feature pattern — no per-feature .csproj).
        if (g.GenerateModule && contract.Architecture.UseFluentValidation)
        {
            if (!await InstallPackageAsync(new PackageEntry("FluentValidation.AspNetCore", "11.*", p.ApiProjectPath)))
                return 1;
        }

        if (HasBlockingWarnings())
        {
            foreach (var warning in _warnings)
                Console.Error.WriteLine($"  ERROR {warning}");
            return 1;
        }

        if (contract.Output.IncludeManifest)
            await WriteManifestAsync();

        Console.WriteLine($"\nDone — {_filesCreated.Count} created, {_filesSkipped.Count} skipped, {_warnings.Count} warnings.");
        return 0;
    }

    private async Task<bool> RenderAndWriteAsync(string templateName, string outputPath, ScriptObject model)
    {
        var templateFile = Path.Combine(templatesPath, templateName);
        if (!File.Exists(templateFile))
        {
            _warnings.Add($"Template not found: {templateFile}");
            Console.Error.WriteLine($"  ERROR template missing: {templateName}");
            return false;
        }

        if (File.Exists(outputPath))
        {
            if (contract.Output.OverwriteExisting)
                _warnings.Add($"OverwriteExisting=true ignored by policy for existing file: {outputPath}");

            _filesSkipped.Add(outputPath);
            Console.WriteLine($"  SKIP  {outputPath}");
            return true;
        }

        var templateText = await File.ReadAllTextAsync(templateFile);
        var template = Template.Parse(templateText);

        if (template.HasErrors)
        {
            foreach (var err in template.Messages)
                _warnings.Add($"[{templateName}] {err}");
            Console.Error.WriteLine($"  ERROR parsing {templateName}");
            return false;
        }

        var context = new TemplateContext { StrictVariables = true };
        context.PushGlobal(model);
        string rendered;
        try
        {
            rendered = await template.RenderAsync(context);
        }
        catch (Exception ex)
        {
            _warnings.Add($"[{templateName}] {ex.Message}");
            Console.Error.WriteLine($"  ERROR rendering {templateName}");
            return false;
        }

        rendered = NormalizeRenderedContent(rendered);

        if (IsBackendCsproj(outputPath) && !ValidateGeneratedCsproj(outputPath, rendered))
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        await File.WriteAllTextAsync(outputPath, rendered, Utf8NoBom);
        _filesCreated.Add(outputPath);
        Console.WriteLine($"  CREATE {outputPath}");
        return true;
    }

    private static string NormalizeRenderedContent(string content)
    {
        // Normalize line endings to avoid mixed whitespace behavior across platforms.
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');

        // Remove trailing spaces/tabs from each line.
        normalized = Regex.Replace(normalized, @"[ \t]+\n", "\n");

        // Keep at most one blank line between blocks.
        normalized = Regex.Replace(normalized, @"\n{3,}", "\n\n");

        return normalized.Replace("\n", Environment.NewLine);
    }

    private ScriptObject BuildModel()
    {
        var f = contract.Feature;
        var p = contract.Project;
        var a = contract.Architecture;
        var w = contract.WorkItem;

        var model = new ScriptObject();
        model["feature_name"]      = f.Name;
        model["entity_name"]       = f.EntityName;
        model["route_base"]        = f.RouteBase;
        model["module_name"]       = f.ModuleName;
        model["target_framework"]  = p.TargetFramework;
        model["namespace_root"]    = p.NamespaceRoot;
        model["application_namespace"] = p.ApplicationNamespace;
        model["result_namespace"] = p.ResultNamespace;
        model["repository_namespace"] = p.RepositoryNamespace;
        model["infrastructure_namespace"] = p.InfrastructureNamespace;
        model["generate_repository"] = contract.Generation.GenerateRepository;
        model["domain_namespace"] = p.DomainNamespace;
        model["test_namespace"] = p.TestNamespace;
        model["db_context"]      = a.DbContext;
        model["use_no_tracking"]          = a.UseAsNoTrackingForReads;
        model["use_service_interface"]    = a.UseServiceInterface;
        model["use_repository_interface"] = a.UseRepositoryInterface;
        model["task_id"]         = w.TaskId;
        model["user_story_id"]   = w.UserStoryId;
        var pkName    = f.PrimaryKey?.Name ?? "Id";
        var pkType    = f.PrimaryKey?.Type ?? "Guid";
        model["pk_column_name"] = ToSnakeCase(pkName);
        var pkDefault = pkType switch {
            "int"  => "1",
            "long" => "1L",
            "Guid" => "Guid.NewGuid()",
            _      => "default"
        };
        model["pk_name"]    = pkName;
        model["pk_param_name"] = ToCamelCase(pkName);
        model["pk_type"]    = pkType;
        model["pk_default"] = pkDefault;
        model["operations"] = MapOperations(pkType);
        model["fields"]     = MapFields();
        model["validation_rules"] = MapRules();

        var recordLock = NormalizeRecordLock(contract.DataModel?.RecordLock);
        model["record_lock_kind"]   = recordLock.Kind;
        model["record_lock_parent"] = recordLock.Parent;
        model["record_lock_fk"]     = recordLock.Fk;
        model["record_lock_ownerless_not_lockable"] = recordLock.OwnerlessNotLockable;
        model["record_lock_reason"] = recordLock.Reason;

        if (contract.DataModel is { } dm)
        {
            model["table_name"]       = dm.TableName;
            model["schema_name"]      = dm.Schema ?? string.Empty;
            model["primary_key"]      = dm.PrimaryKey;
            var dmPkType = contract.Fields
                .FirstOrDefault(f => string.Equals(f.Name, dm.PrimaryKey, StringComparison.OrdinalIgnoreCase))
                ?.Type ?? "Guid";
            model["primary_key_type"] = dmPkType;
            var indexes = new ScriptArray();
            foreach (var idx in dm.Indexes ?? [])
            {
                var o = new ScriptObject();
                var cols = new ScriptArray();
                foreach (var c in idx.Fields) cols.Add(c);
                o["fields"]    = cols;
                o["is_unique"] = idx.IsUnique;
                indexes.Add(o);
            }
            model["indexes"] = indexes;
        }

        return model;
    }

    private List<string> ValidateContract()
    {
        var errors = new List<string>();
        var p = contract.Project;
        var f = contract.Feature;

        ValidateRequired(p.ResultNamespace, nameof(p.ResultNamespace), errors);
        ValidateRequired(p.ApplicationNamespace, nameof(p.ApplicationNamespace), errors);
        ValidateRequired(p.RepositoryNamespace, nameof(p.RepositoryNamespace), errors);
        ValidateRequired(p.DomainNamespace, nameof(p.DomainNamespace), errors);
        ValidateRequired(p.InfrastructureNamespace, nameof(p.InfrastructureNamespace), errors);
        ValidateRequired(p.ApplicationRoot, nameof(p.ApplicationRoot), errors);
        ValidateRequired(p.DomainRoot, nameof(p.DomainRoot), errors);
        ValidateRequired(p.InfrastructureRoot, nameof(p.InfrastructureRoot), errors);
        ValidateRequired(p.TestRoot, nameof(p.TestRoot), errors);
        ValidateRequired(p.TestNamespace, nameof(p.TestNamespace), errors);

        if (!PathsEqualNormalized(f.FeatureRoot, p.ApplicationRoot))
            errors.Add($"Feature.FeatureRoot ('{f.FeatureRoot}') must match Project.ApplicationRoot ('{p.ApplicationRoot}').");

        // Flat feature-based structure: all paths are fixed relative to backendRoot.
        var expectedAppRoot   = PathCombineUnix(p.BackendRoot, "src", "ApiHost", "Features", f.Name);
        var expectedDomainRoot = PathCombineUnix(p.BackendRoot, "src", "Infrastructure", "Database", "Models");
        var expectedInfraRoot  = PathCombineUnix(p.BackendRoot, "src", "Infrastructure", "Database");
        var expectedTestPrefix = PathCombineUnix(p.BackendRoot, "tests", "Backend.Tests", "Features");

        if (!PathsEqualNormalized(p.ApplicationRoot, expectedAppRoot))
            errors.Add($"Project.ApplicationRoot must be '{expectedAppRoot}' for feature '{f.Name}'.");
        if (!PathsEqualNormalized(p.DomainRoot, expectedDomainRoot))
            errors.Add($"Project.DomainRoot must be '{expectedDomainRoot}'.");
        if (!PathsEqualNormalized(p.InfrastructureRoot, expectedInfraRoot))
            errors.Add($"Project.InfrastructureRoot must be '{expectedInfraRoot}'.");
        if (!IsPathUnder(p.TestRoot, expectedTestPrefix))
            errors.Add($"Project.TestRoot must be under '{expectedTestPrefix}'.");

        if ((contract.Generation.GenerateEntity || contract.Generation.GenerateEntityConfiguration)
            && contract.DataModel is null)
            errors.Add("DataModel is required when generateEntity or generateEntityConfiguration is true.");

        if (contract.DataModel is { } dm)
        {
            if (string.IsNullOrWhiteSpace(dm.TableName))
                errors.Add("DataModel.TableName is required.");
            if (string.IsNullOrWhiteSpace(dm.PrimaryKey))
                errors.Add("DataModel.PrimaryKey is required.");

            if (contract.Generation.GenerateEntity || contract.Generation.GenerateEntityConfiguration)
                ValidateRecordLock(dm.RecordLock, contract.Feature.EntityName, errors);
        }

        return errors;
    }

    private const string RecordLockHelp =
        "Use { \"kind\": \"record\" }, { \"kind\": \"partOf\", \"parent\": \"Client\", \"fk\": \"ClientId\" } " +
        "or { \"kind\": \"notLockable\", \"reason\": \"area: why\" }. See .github/skills/record-locking/SKILL.md.";

    private static void ValidateRecordLock(RecordLockSpec? recordLock, string entityName, ICollection<string> errors)
    {
        if (recordLock is null)
        {
            errors.Add($"DataModel.RecordLock is required for entity '{entityName}'. {RecordLockHelp}");
            return;
        }

        switch (NormalizeRecordLockKind(recordLock.Kind))
        {
            case "record":
                break;
            case "partOf":
                if (string.IsNullOrWhiteSpace(recordLock.Parent))
                    errors.Add($"DataModel.RecordLock.Parent is required for partOf on entity '{entityName}'.");
                if (string.IsNullOrWhiteSpace(recordLock.Fk))
                    errors.Add($"DataModel.RecordLock.Fk is required for partOf on entity '{entityName}'.");
                if (!string.IsNullOrWhiteSpace(recordLock.OwnerlessIs)
                    && !string.Equals(recordLock.OwnerlessIs.Trim(), "notLockable", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"DataModel.RecordLock.OwnerlessIs must be \"notLockable\" or omitted on entity '{entityName}'.");
                break;
            case "notLockable":
                if (string.IsNullOrWhiteSpace(recordLock.Reason))
                    errors.Add($"DataModel.RecordLock.Reason is required for notLockable on entity '{entityName}'.");
                break;
            default:
                errors.Add($"DataModel.RecordLock.Kind '{recordLock.Kind}' is not valid for entity '{entityName}'. {RecordLockHelp}");
                break;
        }
    }

    private static string? NormalizeRecordLockKind(string? kind)
        => kind?.Trim().ToLowerInvariant() switch
        {
            "record" => "record",
            "partof" => "partOf",
            "notlockable" => "notLockable",
            _ => null
        };

    private static (string Kind, string Parent, string Fk, bool OwnerlessNotLockable, string Reason) NormalizeRecordLock(RecordLockSpec? recordLock)
    {
        if (recordLock is null)
            return (string.Empty, string.Empty, string.Empty, false, string.Empty);

        return (
            NormalizeRecordLockKind(recordLock.Kind) ?? string.Empty,
            recordLock.Parent?.Trim() ?? string.Empty,
            recordLock.Fk?.Trim() ?? string.Empty,
            string.Equals(recordLock.OwnerlessIs?.Trim(), "notLockable", StringComparison.OrdinalIgnoreCase),
            (recordLock.Reason?.Trim() ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\""));
    }

    private static void ValidateRequired(string value, string name, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"Project.{name} is required and cannot be empty.");
    }

    private static string PathCombineUnix(params string[] parts)
        => string.Join("/", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim().Trim('/').Trim('\\')));

    private static bool PathsEqualNormalized(string left, string right)
        => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static bool IsPathUnder(string path, string root)
    {
        var normalizedPath = NormalizePath(path);
        var normalizedRoot = NormalizePath(root);
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        return normalizedPath.Length == normalizedRoot.Length || normalizedPath[normalizedRoot.Length] == '/';
    }

    private static string NormalizePath(string path)
        => (path ?? string.Empty).Replace('\\', '/').Trim().Trim('/');

    private static bool IsBackendCsproj(string outputPath)
    {
        var normalized = NormalizePath(outputPath).ToLowerInvariant();
        return normalized.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            && (normalized.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/backend/", StringComparison.OrdinalIgnoreCase));
    }

    private bool ValidateGeneratedCsproj(string outputPath, string rendered)
    {
        var normalizedOutputPath = NormalizePath(outputPath);
        var applicationRoot = NormalizePath(contract.Project.ApplicationRoot);
        var expectedApplicationCsproj = NormalizePath(PathCombineUnix(contract.Project.ApplicationRoot, $"{contract.Feature.ModuleName}.Application.csproj"));

        if (normalizedOutputPath.StartsWith(applicationRoot + "/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedOutputPath, expectedApplicationCsproj, StringComparison.OrdinalIgnoreCase))
        {
            _warnings.Add($"Application project must be '{expectedApplicationCsproj}', found '{normalizedOutputPath}'.");
            Console.Error.WriteLine($"  ERROR invalid application csproj path: {normalizedOutputPath}");
            return false;
        }

        var lowered = rendered.ToLowerInvariant();
        if (ForbiddenPathFragments.Any(fragment => lowered.Contains(fragment.Trim().ToLowerInvariant())))
        {
            _warnings.Add($"Generated backend .csproj contains forbidden reference path fragments: {outputPath}");
            Console.Error.WriteLine($"  ERROR forbidden backend reference in csproj: {outputPath}");
            return false;
        }

        return true;
    }

    private ScriptArray MapOperations(string pkType)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var arr = new ScriptArray();
        foreach (var op in contract.Operations)
        {
            var key = $"{op.ServiceMethod}|{op.HttpMethod}|{op.Route}";
            if (!seen.Add(key))
                continue;

            var o = new ScriptObject();
            var hasRouteId = HasRouteIdToken(op.Route);
            o["name"]               = op.Name;
            o["http_method"]        = op.HttpMethod;
            o["route"]              = NormalizeRouteForAttribute(op.Route, pkType);
            o["has_route_id"]       = hasRouteId;
            o["request_dto"]        = string.IsNullOrEmpty(op.RequestDto) ? null : (object)op.RequestDto; // empty string → null so {{ if op.request_dto }} is falsy
            o["response_dto"]       = NormalizeResponseDtoType(op.ResponseDto);
            o["service_method"]     = op.ServiceMethod;
            o["response_is_collection"] = IsCollectionResponse(op.ResponseDto) ? (object)true : false;
            o["success_status"]     = op.SuccessStatus;
            o["responses"]          = MapOperationResponses(op);
            // Per-operation response DTO fields (overrides global fields[] when present).
            if (op.ResponseDtoFields is { Count: > 0 } rdf)
            {
                var rdfArr = new ScriptArray();
                foreach (var rf in rdf)
                {
                    var rf_o = new ScriptObject();
                    rf_o["name"]     = rf.Name;
                    rf_o["type"]     = rf.Type;
                    rf_o["required"] = rf.Required;
                    rdfArr.Add(rf_o);
                }
                o["response_dto_fields"] = rdfArr;
            }
            var methods = new ScriptArray();
            foreach (var m in op.RepositoryMethods.Distinct(StringComparer.OrdinalIgnoreCase)) methods.Add(m);
            o["repository_methods"] = methods;
            arr.Add(o);
        }
        return arr;
    }

    private static string NormalizeRouteForAttribute(string? route, string pkType)
    {
        if (string.IsNullOrWhiteSpace(route))
            return string.Empty;

        var trimmed = route.Trim();
        var pathOnly = trimmed;

        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                pathOnly = uri.AbsolutePath;
        }

        // Controller already has [Route("api/...")) so method route should be relative.
        if (pathOnly.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = pathOnly.Trim('/').Split('/');
            if (segments.Length > 2)
            {
                var relative = string.Join('/', segments.Skip(2));
                if (string.Equals(relative, "{id}", StringComparison.OrdinalIgnoreCase))
                {
                    var constraint = pkType.ToLowerInvariant() switch {
                        "guid" => "guid",
                        "int"  => "int",
                        "long" => "long",
                        _      => null
                    };
                    pathOnly = constraint is not null ? $"{{id:{constraint}}}" : "{id}";
                }
                else
                {
                    pathOnly = relative;
                }
            }
            else
            {
                pathOnly = string.Empty;
            }
        }

        return pathOnly.Trim('/');
    }

    private static string NormalizeResponseDtoType(string responseDto)
    {
        if (string.IsNullOrWhiteSpace(responseDto))
            return "object";

        var trimmed = responseDto.Trim();
        if (trimmed.EndsWith("[]", StringComparison.Ordinal))
            return $"IReadOnlyList<{trimmed[..^2]}>";

        return trimmed;
    }

    private static bool IsCollectionResponse(string responseDto)
        => !string.IsNullOrWhiteSpace(responseDto) && responseDto.Trim().EndsWith("[]", StringComparison.Ordinal);

    private static bool HasRouteIdToken(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return false;

        return Regex.IsMatch(route, @"\{id(?::[^}]*)?\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private bool HasBlockingWarnings()
    {
        return _warnings.Any(w =>
            w.Contains("Template not found", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("Error", StringComparison.OrdinalIgnoreCase));
    }

    private ScriptArray MapFields()
    {
        var arr = new ScriptArray();
        foreach (var f in contract.Fields)
        {
            var o = new ScriptObject();
            o["name"]        = f.Name;
            o["column_name"] = ToSnakeCase(f.Name);
            o["type"]        = f.Type;
            o["required"]    = f.Required;
            o["max_length"]  = f.MaxLength;
            arr.Add(o);
        }
        return arr;
    }

    private static string ToSnakeCase(string name)
        => Regex.Replace(name, @"(?<=[a-z0-9])(?=[A-Z])", "_").ToLowerInvariant();

    private ScriptArray MapRules()
    {
        var fieldTypeMap = contract.Fields.ToDictionary(f => f.Name, f => f.Type, StringComparer.OrdinalIgnoreCase);
        var arr = new ScriptArray();
        foreach (var r in contract.Validation.Rules)
        {
            var o = new ScriptObject();
            o["field"]      = r.Field;
            o["rule"]       = r.Rule;
            o["message"]    = r.Message;
            o["max_length"] = r.MaxLength;
            o["field_type"] = fieldTypeMap.TryGetValue(r.Field, out var t) ? t : "string";
            arr.Add(o);
        }
        return arr;
    }

    private async Task<bool> InstallPackageAsync(PackageEntry pkg)
    {
        if (!File.Exists(pkg.Project))
        {
            Console.Error.WriteLine($"  ERROR .csproj not found: {pkg.Project}");
            return false;
        }

        // Always run dotnet add package — it is idempotent when the version already
        // satisfies the constraint, and it upgrades when the pinned version conflicts
        // with a transitive dependency (e.g. FluentValidation base vs AspNetCore wrapper).
        var versionArg = pkg.Version is not null ? $"--version {pkg.Version}" : "";
        var exitCode   = await RunAsync("dotnet", $"add \"{pkg.Project}\" package {pkg.Name} {versionArg}".Trim());
        if (exitCode == 0)
        {
            _packagesInstalled.Add(pkg.Name);
            Console.WriteLine($"  PKG   {pkg.Name} added/verified");
            return true;
        }

        Console.Error.WriteLine($"  ERROR installing package {pkg.Name} (exit {exitCode})");
        return false;
    }

    private static async Task<int> RunAsync(string exe, string arguments)
    {
        var psi = new ProcessStartInfo(exe, arguments) { UseShellExecute = false };
        using var p = Process.Start(psi)!;
        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    private async Task WriteManifestAsync()
    {
        var manifest = new CodegenManifest(
            contract.WorkItem.TaskId,
            contract.WorkItem.UserStoryId,
            DateTime.UtcNow.ToString("O"),
            _filesCreated,
            _filesSkipped,
            _filesModified,
            _packagesInstalled,
            _packagesSkipped,
            _warnings,
            $"{_filesCreated.Count} file(s) created, {_filesSkipped.Count} skipped, {_warnings.Count} warning(s).",
            BuildGeneratedSymbols()
        );

        var path    = Path.Combine(_contractDir, "backend-codegen-manifest.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest, options), Utf8NoBom);
        Console.WriteLine($"  MANIFEST {path}");
    }

    // ── Symbol derivation ──────────────────────────────────────────────────────
    // All symbols are derived purely from the contract — no file parsing required.
    // The backend-agent reads generatedSymbols once and has the full picture of
    // what was generated without opening any individual source file.

    private GeneratedSymbols BuildGeneratedSymbols()
    {
        var f = contract.Feature;
        var p = contract.Project;
        var g = contract.Generation;

        var entityPath      = PathCombineUnix(p.DomainRoot, $"{f.EntityName}.cs");
        var repositoryPath  = PathCombineUnix(f.FeatureRoot, $"{f.EntityName}Repository.cs");
        var dtosPath        = PathCombineUnix(f.FeatureRoot, "Dtos", $"{f.EntityName}Dtos.cs");
        var servicePath         = PathCombineUnix(f.FeatureRoot, $"{f.EntityName}Service.cs");
        var validatorPath       = PathCombineUnix(f.FeatureRoot, $"{f.EntityName}Validator.cs");
        var controllerPath      = PathCombineUnix(f.FeatureRoot, $"{f.Name}Controller.cs");
        var modulePath          = PathCombineUnix(f.FeatureRoot, $"{f.ModuleName}.cs");

        var pkName = f.PrimaryKey?.Name ?? "Id";
        var pkType = f.PrimaryKey?.Type ?? "Guid";

        EntitySymbol? entity = g.GenerateEntity ? BuildEntitySymbol(f, p, entityPath, pkName, pkType) : null;

        var dtos = g.GenerateDtos
            ? BuildDtoSymbols(f, p, dtosPath, pkName, pkType)
            : [];

        ServiceSymbol? service = g.GenerateService
            ? BuildServiceSymbol(f, p, servicePath, pkType)
            : null;

        RepositorySymbol? repository = g.GenerateRepository
            ? BuildRepositorySymbol(f, p, repositoryPath)
            : null;

        ValidatorSymbol? validator = g.GenerateValidator
            ? BuildValidatorSymbol(f, p, validatorPath)
            : null;

        ControllerSymbol? controller = g.GenerateController
            ? BuildControllerSymbol(f, p, controllerPath, pkType)
            : null;

        ModuleSymbol? module = g.GenerateModule
            ? BuildModuleSymbol(f, p, modulePath)
            : null;

        return new GeneratedSymbols(entity, dtos, service, repository, validator, controller, module);
    }

    private EntitySymbol BuildEntitySymbol(Feature f, Project p, string filePath, string pkName, string pkType)
    {
        var properties = contract.Fields
            .Select(fi => new SymbolProp(fi.Name, fi.Type, !fi.Required))
            .ToList();

        var ctorParams = new List<SymbolParam> { new(ToCamelCase(pkName), pkType, false) };
        ctorParams.AddRange(contract.Fields.Select(fi => new SymbolParam(ToCamelCase(fi.Name), fi.Type, !fi.Required)));

        return new EntitySymbol(f.EntityName, p.DomainNamespace, filePath, properties, ctorParams);
    }

    private List<DtoSymbol> BuildDtoSymbols(Feature f, Project p, string filePath, string pkName, string pkType)
    {
        var dtoNamespace = $"{p.ApplicationNamespace}.Dtos";
        var result = new List<DtoSymbol>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var op in contract.Operations)
        {
            if (!string.IsNullOrEmpty(op.RequestDto) && seen.Add($"req:{op.RequestDto}"))
            {
                var reqParams = contract.Fields
                    .Select(fi => new SymbolParam(fi.Name, fi.Type, !fi.Required))
                    .ToList();
                result.Add(new DtoSymbol(op.RequestDto!, "Request", dtoNamespace, filePath, reqParams));
            }

            if (!string.IsNullOrEmpty(op.ResponseDto) && seen.Add($"resp:{op.ResponseDto}"))
            {
                var respFields = op.ResponseDtoFields is { Count: > 0 } rdf ? rdf : contract.Fields;
                var respParams = new List<SymbolParam> { new(pkName, pkType, false) };
                respParams.AddRange(respFields.Select(fi => new SymbolParam(fi.Name, fi.Type, !fi.Required)));
                result.Add(new DtoSymbol(op.ResponseDto, "Response", dtoNamespace, filePath, respParams));
            }
        }

        return result;
    }

    private ServiceSymbol BuildServiceSymbol(Feature f, Project p, string filePath, string pkType)
    {
        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var methods = new List<MethodSymbol>();

        foreach (var op in contract.Operations)
        {
            var key = $"{op.ServiceMethod}|{op.HttpMethod}|{op.Route}";
            if (!seen.Add(key)) continue;

            var returnType = $"Task<Result<{NormalizeResponseDtoType(op.ResponseDto)}>>";
            var parameters = new List<SymbolParam>();
            var hasRouteId = HasRouteIdToken(op.Route);
            var pkParamName = ToCamelCase(f.PrimaryKey?.Name ?? "Id");

            if (hasRouteId)
                parameters.Add(new SymbolParam(pkParamName, pkType, false));

            if (!string.IsNullOrEmpty(op.RequestDto))
                parameters.Add(new SymbolParam("request", op.RequestDto!, false));

            parameters.Add(new SymbolParam("cancellationToken", "CancellationToken", false));
            methods.Add(new MethodSymbol(op.ServiceMethod, returnType, parameters));
        }

        var useSvcIface   = contract.Architecture.UseServiceInterface;
        var interfaceName = useSvcIface ? $"I{f.EntityName}Service" : string.Empty;
        return new ServiceSymbol(interfaceName, $"{f.EntityName}Service", p.ApplicationNamespace, filePath, methods);
    }

    private RepositorySymbol BuildRepositorySymbol(Feature f, Project p, string implPath)
    {
        var uniqueMethods = contract.Operations
            .SelectMany(op => op.RepositoryMethods)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var pkType = f.PrimaryKey?.Type ?? "Guid";
        var methods = uniqueMethods.Select(m => BuildRepoMethodSymbol(m, f.EntityName, pkType)).ToList();

        var useRepoIface = contract.Architecture.UseRepositoryInterface;
        return new RepositorySymbol(
            InterfaceName: useRepoIface ? $"I{f.EntityName}Repository" : string.Empty,
            ClassName: $"{f.EntityName}Repository",
            InterfaceNamespace: useRepoIface ? p.RepositoryNamespace : string.Empty,
            ImplementationNamespace: p.RepositoryNamespace,
            InterfaceFilePath: useRepoIface ? PathCombineUnix(f.FeatureRoot, $"I{f.EntityName}Repository.cs") : string.Empty,
            ImplementationFilePath: implPath,
            Methods: methods);
    }

    private static MethodSymbol BuildRepoMethodSymbol(string name, string entityName, string pkType) =>
        name.ToLowerInvariant() switch
        {
            "addasync" => new("AddAsync", "Task",
                [new("entity", entityName, false), new("ct", "CancellationToken", false)]),
            "savechangesasync" => new("SaveChangesAsync", "Task",
                [new("ct", "CancellationToken", false)]),
            "getbyidasync" => new($"GetByIdAsync", $"Task<{entityName}?>",
                [new("id", pkType, false), new("ct", "CancellationToken", false)]),
            _ => new(name, "Task",
                [new("ct", "CancellationToken", false)])
        };

    private ValidatorSymbol BuildValidatorSymbol(Feature f, Project p, string filePath)
    {
        var requestDto = contract.Operations
            .Where(op => !string.IsNullOrEmpty(op.RequestDto))
            .Select(op => op.RequestDto!)
            .FirstOrDefault() ?? $"Create{f.EntityName}Request";

        var fields = contract.Validation.Rules
            .Select(r => r.Field)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ValidatorSymbol($"{f.EntityName}Validator", p.ApplicationNamespace, filePath, requestDto, fields);
    }

    private ControllerSymbol BuildControllerSymbol(Feature f, Project p, string filePath, string pkType)
    {
        var actions = contract.Operations.Select(op => new ActionSymbol(
            $"{op.Name}Async",
            op.HttpMethod,
            NormalizeRouteForAttribute(op.Route, pkType),
            string.IsNullOrEmpty(op.RequestDto) ? null : op.RequestDto,
            op.ResponseDto,
            op.SuccessStatus,
            op.Responses?.Select(r => new ActionResponseSymbol(
                r.StatusCode,
                NormalizePayloadType(r.PayloadType),
                string.IsNullOrWhiteSpace(r.Description) ? null : r.Description,
                string.IsNullOrWhiteSpace(r.ContentType) ? null : r.ContentType
            )).ToList())).ToList();

        return new ControllerSymbol($"{f.Name}Controller", p.ApplicationNamespace, filePath, f.RouteBase, actions);
    }

    private ModuleSymbol BuildModuleSymbol(Feature f, Project p, string filePath)
    {
        var requestDto = contract.Operations
            .Where(op => !string.IsNullOrEmpty(op.RequestDto))
            .Select(op => op.RequestDto!)
            .FirstOrDefault() ?? $"Create{f.EntityName}Request";

        var useSvcIface  = contract.Architecture.UseServiceInterface;
        var useRepoIface = contract.Architecture.UseRepositoryInterface;
        var registered   = new List<string>
        {
            useRepoIface ? $"I{f.EntityName}Repository → {f.EntityName}Repository" : $"{f.EntityName}Repository",
            $"{f.EntityName}Validator",
            useSvcIface  ? $"I{f.EntityName}Service → {f.EntityName}Service"        : $"{f.EntityName}Service",
        };

        return new ModuleSymbol(f.ModuleName, p.ApplicationNamespace, filePath, $"Add{f.Name}Module", registered);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private ScriptArray MapOperationResponses(Operation op)
    {
        var responses = new ScriptArray();
        var seen = new HashSet<int>();

        if (op.Responses is { Count: > 0 })
        {
            foreach (var response in op.Responses)
            {
                if (!seen.Add(response.StatusCode))
                    continue;

                responses.Add(ToResponseScriptObject(response));
            }

            // Generated controllers map NotFound-like errors to HTTP 404 at runtime.
            // Ensure metadata stays aligned with runtime behavior when 404 is omitted.
            if (!seen.Contains(404))
            {
                responses.Add(ToResponseScriptObject(new OperationResponse(
                    404,
                    "ProblemDetails",
                    null,
                    null)));
            }

            return responses;
        }

        responses.Add(ToResponseScriptObject(new OperationResponse(
            op.SuccessStatus,
            NormalizeResponseDtoType(op.ResponseDto),
            null,
            null)));
        responses.Add(ToResponseScriptObject(new OperationResponse(
            400,
            "ProblemDetails",
            null,
            null)));
        responses.Add(ToResponseScriptObject(new OperationResponse(
            404,
            "ProblemDetails",
            null,
            null)));
        return responses;
    }

    private static ScriptObject ToResponseScriptObject(OperationResponse response)
    {
        var responseObj = new ScriptObject();
        var payloadType = NormalizePayloadType(response.PayloadType);
        responseObj["status_code"] = response.StatusCode;
        responseObj["payload_type"] = payloadType;
        responseObj["has_payload"] = payloadType is not null;
        responseObj["description"] = string.IsNullOrWhiteSpace(response.Description) ? null : response.Description;
        responseObj["content_type"] = string.IsNullOrWhiteSpace(response.ContentType) ? null : response.ContentType;
        return responseObj;
    }

    private static string? NormalizePayloadType(string? payloadType)
    {
        if (string.IsNullOrWhiteSpace(payloadType))
            return null;

        return NormalizeResponseDtoType(payloadType);
    }
}
