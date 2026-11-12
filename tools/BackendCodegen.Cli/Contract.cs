namespace BackendCodegen.Cli;

public record BackendGenerationContract(
    WorkItem WorkItem,
    Project Project,
    Feature Feature,
    Architecture Architecture,
    List<Operation> Operations,
    List<Field> Fields,
    Validation Validation,
    GenerationFlags Generation,
    List<PackageEntry>? Packages,
    OutputOptions Output,
    DataModel? DataModel = null
);

public record WorkItem(
    string UserStoryId,
    string TaskId,
    string Title,
    string Classification
);

public record Project(
    string BackendRoot,
    string TargetFramework,
    string ApiProjectPath,
    string TestProjectPath,
    string NamespaceRoot,
    string ResultNamespace,
    string ApplicationNamespace,
    string RepositoryNamespace,
    string DomainNamespace,
    string InfrastructureNamespace,
    string ApplicationRoot,
    string DomainRoot,
    string InfrastructureRoot,
    string TestRoot,
    string TestNamespace
);

public record PrimaryKey(string Name, string Type);

public record Feature(
    string Name,
    string EntityName,
    string RouteBase,
    string FeatureRoot,
    string ModuleName,
    PrimaryKey? PrimaryKey = null
);

public record Architecture(
    string Pattern,
    string DbContext,
    string ResultType,
    bool UseAsyncOnly,
    bool UseAsNoTrackingForReads,
    bool UseFluentValidation,
    bool AvoidPiiLogging,
    bool UseServiceInterface = false,
    bool UseRepositoryInterface = false
);

public record Operation(
    string Name,
    string HttpMethod,
    string Route,
    string? RequestDto,
    string ResponseDto,
    string ServiceMethod,
    List<string> RepositoryMethods,
    int SuccessStatus,
    List<OperationResponse>? Responses,
    List<int>? CoveredAcceptanceCriteria,
    // Optional: when present, the response DTO only includes these fields instead of all entity fields.
    List<Field>? ResponseDtoFields = null
);

public record OperationResponse(
    int StatusCode,
    string? PayloadType,
    string? Description,
    string? ContentType
);

public record Field(
    string Name,
    string Type,
    bool Required,
    int MaxLength
);

public record ValidationRule(
    string Field,
    string Rule,
    string Message,
    [property: System.Text.Json.Serialization.JsonPropertyName("max_length")]
    int MaxLength
);

public record Validation(
    string Library,
    List<ValidationRule> Rules
);

public record GenerationFlags(
    bool GenerateController = false,
    bool GenerateService = false,
    bool GenerateRepository = false,
    bool GenerateValidator = false,
    bool GenerateDtos = false,
    bool GenerateModule = false,
    bool GenerateTests = false,
    bool GenerateReadme = false,
    bool GenerateEntity = false,
    bool GenerateEntityConfiguration = false,
    bool GenerateApplicationProject = false
);

public record DataModel(
    string TableName,
    string? Schema,
    string PrimaryKey,
    List<DataIndex>? Indexes,
    // Required when generateEntity or generateEntityConfiguration is true.
    // See .github/skills/record-locking/SKILL.md.
    RecordLockSpec? RecordLock = null
);

/// <summary>
/// Record-lock classification of the generated entity (US 11038).
/// kind = "record" | "partOf" | "notLockable".
/// partOf requires parent (root or owned entity class name) and fk (FK property);
/// ownerlessIs = "notLockable" allows rows whose FK is null.
/// notLockable requires a non-empty reason ("area: why").
/// </summary>
public record RecordLockSpec(
    string Kind,
    string? Parent = null,
    string? Fk = null,
    string? OwnerlessIs = null,
    string? Reason = null
);

public record DataIndex(
    List<string> Fields,
    bool IsUnique
);

public record PackageEntry(
    string Name,
    string? Version,
    string Project
);

public record OutputOptions(
    string Mode,
    bool OverwriteExisting,
    bool IncludeManifest
);

public record CodegenManifest(
    string TaskId,
    string UserStoryId,
    string GeneratedAt,
    List<string> FilesCreated,
    List<string> FilesSkipped,
    List<string> FilesModified,
    List<string> PackagesInstalled,
    List<string> PackagesSkipped,
    List<string> Warnings,
    string Summary,
    GeneratedSymbols? GeneratedSymbols = null
);

// ── Generated Symbols ──────────────────────────────────────────────────────────
// Emitted into the manifest so the backend-agent can plan all edits from one read
// instead of opening every generated file individually.

public record GeneratedSymbols(
    EntitySymbol?     Entity,
    List<DtoSymbol>   Dtos,
    ServiceSymbol?    Service,
    RepositorySymbol? Repository,
    ValidatorSymbol?  Validator,
    ControllerSymbol? Controller,
    ModuleSymbol?     Module
);

public record SymbolParam(string Name, string Type, bool IsNullable);
public record SymbolProp(string Name, string Type, bool IsNullable);

public record EntitySymbol(
    string ClassName,
    string Namespace,
    string FilePath,
    List<SymbolProp>  Properties,
    List<SymbolParam> ConstructorParams
);

public record DtoSymbol(
    string RecordName,
    string Kind,
    string Namespace,
    string FilePath,
    List<SymbolParam> Parameters
);

public record MethodSymbol(
    string Name,
    string ReturnType,
    List<SymbolParam> Parameters
);

public record ServiceSymbol(
    string InterfaceName,
    string ClassName,
    string Namespace,
    string FilePath,
    List<MethodSymbol> Methods
);

public record RepositorySymbol(
    string InterfaceName,
    string ClassName,
    string InterfaceNamespace,
    string ImplementationNamespace,
    string InterfaceFilePath,
    string ImplementationFilePath,
    List<MethodSymbol> Methods
);

public record ValidatorSymbol(
    string ClassName,
    string Namespace,
    string FilePath,
    string ValidatedType,
    List<string> ValidatedFields
);

public record ActionSymbol(
    string MethodName,
    string HttpMethod,
    string Route,
    string? RequestDto,
    string ResponseDto,
    int SuccessStatus,
    List<ActionResponseSymbol>? Responses = null
);

public record ActionResponseSymbol(
    int StatusCode,
    string? PayloadType,
    string? Description,
    string? ContentType
);

public record ControllerSymbol(
    string ClassName,
    string Namespace,
    string FilePath,
    string Route,
    List<ActionSymbol> Actions
);

public record ModuleSymbol(
    string ClassName,
    string Namespace,
    string FilePath,
    string ExtensionMethodName,
    List<string> RegisteredServices
);
