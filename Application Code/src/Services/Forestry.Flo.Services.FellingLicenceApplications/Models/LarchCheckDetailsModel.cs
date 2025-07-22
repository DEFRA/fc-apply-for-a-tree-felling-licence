using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class LarchCheckDetailsModel
{
    public Guid Id { get; set; }
    public Guid FellingLicenceApplicationId { get; set; }
    public bool? ConfirmLarchOnly { get; set; }
    public bool Zone1 { get; set; }
    public bool Zone2 { get; set; }
    public bool Zone3 { get; set; }
    public bool? ConfirmMoratorium { get; set; }
    public bool ConfirmInspectionLog { get; set; }
    public int RecommendSplitApplicationDue { get; set; }
    public DateTime? FlightDate { get; set; }
    public string? FlightObservations { get; set; }

    public void MapToEntity(LarchCheckDetails entity)
    {
        entity.FellingLicenceApplicationId = FellingLicenceApplicationId;
        entity.ConfirmLarchOnly = ConfirmLarchOnly;
        entity.Zone1 = Zone1;
        entity.Zone2 = Zone2;
        entity.Zone3 = Zone3;
        entity.ConfirmMoratorium = ConfirmMoratorium;
        entity.ConfirmInspectionLog = ConfirmInspectionLog;
        entity.RecommendSplitApplicationDue = RecommendSplitApplicationDue;
    }
}

public static class LarchCheckDetailsExtensions
{
    public static LarchCheckDetailsModel ToModel(this LarchCheckDetails entity)
    {
        return new LarchCheckDetailsModel
        {
            Id = entity.Id,
            FellingLicenceApplicationId = entity.FellingLicenceApplicationId,
            ConfirmLarchOnly = entity.ConfirmLarchOnly,
            Zone1 = entity.Zone1,
            Zone2 = entity.Zone2,
            Zone3 = entity.Zone3,
            ConfirmMoratorium = entity.ConfirmMoratorium,
            ConfirmInspectionLog = entity.ConfirmInspectionLog,
            RecommendSplitApplicationDue = entity.RecommendSplitApplicationDue,
            FlightDate = entity.FlightDate, 
            FlightObservations = entity.FlightObservations
        };
    }

    public static DateOnly? ConvertToDateOnly(DateTime? dateTime)
    {
        return dateTime.HasValue ? DateOnly.FromDateTime(dateTime.Value) : null;
    }
}
