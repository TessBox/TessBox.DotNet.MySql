using System.Reflection;
using System.Text;

namespace TessBox.DotNet.MySql;

internal class MigrationItem
{
    public MigrationItem(int version, string filePath)
    {
        Version = version;
        FilePath = filePath;
    }

    public int Version { get; }

    public string FilePath { get; }
}

internal sealed class MigrationItemProvider
{
    private readonly Assembly _scriptAssembly;

    public MigrationItemProvider(Assembly scriptAssembly)
    {
        _scriptAssembly = scriptAssembly;
        Migrations = GetMigrationList();
        Version = Migrations.LastOrDefault()?.Version ?? 0;
    }

    public IEnumerable<MigrationItem> Migrations { get; }

    public int Version { get; }

    public async Task<string> GetScriptFromAsync(int version)
    {
        var files = Migrations.Where(t => t.Version > version);

        var result = new StringBuilder();

        foreach (var item in files)
        {
            result.AppendLine(await _scriptAssembly.ReadResourceAsync(item.FilePath));
        }

        return result.ToString();
    }

    public async Task<string> GetScriptAsync(string name)
    {
        var scriptList = _scriptAssembly
            .GetManifestResourceNames()
            .Where(t => t.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));
        if (!scriptList.Any())
            throw new Exception("Script not found");

        if (scriptList.Count() > 1)
            throw new Exception("The script is not unique");

        var content = await _scriptAssembly.ReadResourceAsync(scriptList.First());
        return content;
    }

    private IEnumerable<MigrationItem> GetMigrationList()
    {
        var scriptList = _scriptAssembly.GetManifestResourceNames().Where(t => t.EndsWith(".sql"));
        var result = new List<MigrationItem>();

        foreach (var script in scriptList)
        {
            var item = ReadMigrationItem(script);
            if (item != null)
            {
                result.Add(item);
            }
        }
        return result.OrderBy(t => t.Version);
    }

    private static MigrationItem? ReadMigrationItem(string filepath)
    {
        var separatorIndex = filepath.IndexOf('_');
        if (separatorIndex < 0)
            return null;

        // extract version from namespace.001_init.sql
        var strVersion = filepath[..separatorIndex];
        strVersion = strVersion[(strVersion.LastIndexOf('.') + 1)..];
        if (!int.TryParse(strVersion, out int version))
            return null;

        return new MigrationItem(version, filepath);
    }
}
