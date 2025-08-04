namespace MigrationService.Services;

public static class ResourcesService
{
    public static string GetResourceFileString(string fileName)
    {
        var assembly = typeof(ResourcesService).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(x => x.EndsWith(fileName));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    public static string GetResourceFileStringWithReplacers(string fileName, Dictionary<string, string> replacers)
    {
        var result = GetResourceFileString(fileName);

        return replacers.Aggregate(result, (current, replacer) => 
            current.Replace(replacer.Key, replacer.Value));
    }
}