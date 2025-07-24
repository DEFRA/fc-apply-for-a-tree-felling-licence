using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public static class TreeSpeciesFactory
{
    static TreeSpeciesFactory()
    {
        var assembly = typeof(TreeSpeciesFactory).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(x => x.EndsWith("species.csv"));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TreeSpeciesModelMap>();
        var records = csv.GetRecords<TreeSpeciesModel>();
        SpeciesDictionary = records
            .OrderBy(r => r.Name)
            .ToDictionary(r => r.Code, r => r);
    }

    public static Dictionary<string, TreeSpeciesModel> SpeciesDictionary { get; }
}

internal sealed class TreeSpeciesModelMap : ClassMap<TreeSpeciesModel>
{
    public TreeSpeciesModelMap()
    {
        Map(m => m.Code).Name("SPECIES");
        Map(m => m.Name).Name("NAME");
        Map(m => m.SpeciesType).Name("SPECIES_TYPE");
        Map(m => m.IsNative).Name("NATIVE").TypeConverter<YesNoConverter>();
        Map(m => m.IsLarch).Name("SPECIES_GROUP").TypeConverter<LarchConverter>();
    }
}

class YesNoConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        => "Yes".StartsWith(text, StringComparison.OrdinalIgnoreCase);
    

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        => value is true ? "Yes" : "No";
}
class LarchConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        => "Larch".Equals(text, StringComparison.OrdinalIgnoreCase);

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        => value is true ? "Larch" : string.Empty;
}




