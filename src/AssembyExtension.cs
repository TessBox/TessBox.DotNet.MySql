using System.Reflection;

namespace TessBox.DotNet.MySql;

public static class AssembyExtension
{
    public static async Task<string> ReadResourceAsync(this Assembly assembly, string resourcePath)
    {
        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }
}
