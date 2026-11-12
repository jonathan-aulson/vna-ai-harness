using System.Text.Json;
using BackendCodegen.Cli;

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("BackendCodegen.Cli 1.0");
    Console.WriteLine("Usage: generate --contract <path> --templates <path>");
    return 0;
}

if (args[0] != "generate")
{
    Console.Error.WriteLine($"Unknown command: {args[0]}. Expected: generate");
    return 1;
}

var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
for (var i = 1; i < args.Length - 1; i += 2)
    flags[args[i]] = args[i + 1];

if (!flags.TryGetValue("--contract", out var contractPath))
{
    Console.Error.WriteLine("Error: --contract <path> is required.");
    return 1;
}
if (!flags.TryGetValue("--templates", out var templatesPath))
{
    Console.Error.WriteLine("Error: --templates <path> is required.");
    return 1;
}
if (!File.Exists(contractPath))
{
    Console.Error.WriteLine($"Error: contract file not found: {contractPath}");
    return 1;
}
if (!Directory.Exists(templatesPath))
{
    Console.Error.WriteLine($"Error: templates directory not found: {templatesPath}");
    return 1;
}

Console.WriteLine($"BackendCodegen.Cli — contract: {contractPath}");

var json     = await File.ReadAllTextAsync(contractPath);
var options  = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var contract = JsonSerializer.Deserialize<BackendGenerationContract>(json, options);

if (contract is null)
{
    Console.Error.WriteLine("Error: failed to deserialize contract.");
    return 1;
}

var generator = new Generator(contract, contractPath, templatesPath);
return await generator.RunAsync();
