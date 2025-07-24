using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.FellingLicenceApplications;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddAppRefIdCountersForYears(this ModelBuilder  modelBuilder, string counterName, int startYear, int endYear)
    {
        if (startYear > endYear)
        {
            throw new ArgumentException("Start year cannot be greater than the end year", nameof(startYear));
        }
        
        
        for (var year = startYear; year <= endYear; year++)
        {
            modelBuilder.HasSequence<long>($"{counterName}{year}");
        }

        return modelBuilder;
    }
}