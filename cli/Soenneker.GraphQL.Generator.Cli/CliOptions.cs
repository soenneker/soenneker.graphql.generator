namespace Soenneker.GraphQL.Generator.Cli;
public sealed class CliOptions
{
    public const string Usage = """
gql2cs - GraphQL SDL to C# source generator

Usage:
  gql2cs --config <path-to-config.json>

Example:
  gql2cs --config ./config.json
""";

    public CliOptions(string[] args)
    {
        Args = args;
    }

    public string[] Args { get; }

    /// <summary>
    /// Attempts to get config path.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <param name="configPath">Path of the config to use.</param>
    /// <param name="error">Error for the try get config path operation.</param>
    /// <param name="showUsage">Whether show usage.</param>
    /// <param name="exitCode">Exit Code for the try get config path operation.</param>
    /// <returns>true if a matching value was found and assigned to the output parameter; otherwise, false.</returns>
    public static bool TryGetConfigPath(string[] args, out string? configPath, out string? error, out bool showUsage, out int exitCode)
    {
        configPath = null;
        error = null;
        showUsage = false;
        exitCode = 0;

        if (args.Length == 0)
        {
            showUsage = true;
            exitCode = 1;
            return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                configPath = args[i + 1];
                i++;
            }
        }

        if (!string.IsNullOrWhiteSpace(configPath))
            return true;

        error = "Missing required --config argument.";
        showUsage = true;
        exitCode = 2;
        return false;
    }
}
