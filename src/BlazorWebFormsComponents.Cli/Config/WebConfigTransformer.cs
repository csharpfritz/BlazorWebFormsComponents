using System.Text.Json;
using System.Xml.Linq;
using BlazorWebFormsComponents.Cli.Io;

namespace BlazorWebFormsComponents.Cli.Config;

/// <summary>
/// Parses Web.config and generates appsettings.json.
/// Ported from Convert-WebConfigToAppSettings in bwfc-migrate.ps1 (line ~892).
/// </summary>
public class WebConfigTransformer
{
    private static readonly HashSet<string> BuiltInConnectionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "LocalSqlServer",
        "LocalMySqlServer"
    };

    /// <summary>
    /// Transforms Web.config appSettings and connectionStrings into appsettings.json content.
    /// Returns null if no Web.config found or no settings to extract.
    /// </summary>
    public WebConfigResult? Transform(string sourcePath)
    {
        // Find Web.config (case-insensitive search)
        var webConfigPath = FindWebConfig(sourcePath);
        if (webConfigPath == null)
            return null;

        XDocument doc;
        try
        {
            doc = XDocument.Load(webConfigPath);
        }
        catch (Exception ex)
        {
            return new WebConfigResult
            {
                JsonContent = null,
                AppSettingsCount = 0,
                ConnectionStringsCount = 0,
                Error = $"Could not parse Web.config: {ex.Message}"
            };
        }

        var appSettings = new Dictionary<string, string>();
        var connectionStrings = new Dictionary<string, string>();

        // Parse <appSettings>
        var appSettingsNodes = doc.Descendants("appSettings").Elements("add");
        foreach (var node in appSettingsNodes)
        {
            var key = node.Attribute("key")?.Value;
            var value = node.Attribute("value")?.Value ?? "";
            if (!string.IsNullOrEmpty(key))
            {
                appSettings[key] = value;
            }
        }

        // Parse <connectionStrings>
        var connStrNodes = doc.Descendants("connectionStrings").Elements("add");
        foreach (var node in connStrNodes)
        {
            var name = node.Attribute("name")?.Value;
            var connStr = node.Attribute("connectionString")?.Value;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(connStr))
            {
                if (!BuiltInConnectionNames.Contains(name))
                {
                    connectionStrings[name] = connStr;
                }
            }
        }

        if (appSettings.Count == 0 && connectionStrings.Count == 0)
            return null;

        // Build JSON structure
        var jsonObj = new Dictionary<string, object>();

        if (connectionStrings.Count > 0)
        {
            jsonObj["ConnectionStrings"] = connectionStrings;
        }

        foreach (var entry in appSettings)
        {
            jsonObj[entry.Key] = entry.Value;
        }

        // Add standard Blazor sections
        if (!jsonObj.ContainsKey("Logging"))
        {
            jsonObj["Logging"] = new Dictionary<string, object>
            {
                ["LogLevel"] = new Dictionary<string, string>
                {
                    ["Default"] = "Information",
                    ["Microsoft.AspNetCore"] = "Warning"
                }
            };
        }

        if (!jsonObj.ContainsKey("AllowedHosts"))
        {
            jsonObj["AllowedHosts"] = "*";
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var jsonContent = JsonSerializer.Serialize(jsonObj, options);

        return new WebConfigResult
        {
            JsonContent = jsonContent,
            AppSettingsCount = appSettings.Count,
            ConnectionStringsCount = connectionStrings.Count,
            AppSettingsKeys = [.. appSettings.Keys],
            ConnectionStringNames = [.. connectionStrings.Keys]
        };
    }

    private static string? FindWebConfig(string sourcePath)
    {
        var path1 = Path.Combine(sourcePath, "Web.config");
        if (File.Exists(path1)) return path1;

        var path2 = Path.Combine(sourcePath, "web.config");
        if (File.Exists(path2)) return path2;

        return null;
    }
}

public class WebConfigResult
{
    public string? JsonContent { get; init; }
    public int AppSettingsCount { get; init; }
    public int ConnectionStringsCount { get; init; }
    public List<string> AppSettingsKeys { get; init; } = [];
    public List<string> ConnectionStringNames { get; init; } = [];
    public string? Error { get; init; }
}
