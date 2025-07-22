using Forestry.Flo.Services.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class LarchCheckModel : FellingLicenceApplicationPageViewModel
{
    [Required(ErrorMessage = "Confirm tree species")]
    public bool? ConfirmLarchOnly { get; set; }

    [AtLeastOneZoneRequired]
    public bool Zone1 { get; set; }
    public bool Zone2 { get; set; }
    public bool Zone3 { get; set; }

    public string? ZonesCombined { get; set; }
    public void CombineZones(List<FellingAndRestockingDetail> DetailsList)
    {
        var zones = new List<string>();
        foreach (var detail in DetailsList)
        {
            if (detail.Zone1) zones.Add("zone 1");
            if (detail.Zone2) zones.Add("zone 2");
            if (detail.Zone3) zones.Add("zone 3");
        }
        ZonesCombined = string.Join(" and ", zones.Distinct().OrderBy(z => z));
    }

    [Required(ErrorMessage = "Check must either pass or fail")]
    public bool? ConfirmMoratorium { get; set; }

    public bool InMoratorium { get; set; }

    [ConfirmInspectionLogRequired]
    public bool ConfirmInspectionLog { get; set; }

    [Required(ErrorMessage = "Recommend split application")]
    public RecommendSplitApplicationEnum? RecommendSplitApplicationDue { get; set; }

    public IEnumerable<FellingSpeciesModel>? AllSpecies { get; set; }

    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    public IList<ActivityFeedItemModel>? ActivityFeedItems { get; set; }

    /// <summary>
    /// Gets a flag indicating whether the application state and current user should be able
    /// to edit the larch details for the application.
    /// </summary>
    public bool Disabled { get; set; }

    public DateTime ExtendedFAD { get; set; }

    public string? FlyoverPeriod { get; set; }

    public string? MoratoriumPeriod { get; set; }

    public string? CaseNote { get; set; }
    public bool VisibleToApplicant { get; set; }
    public bool VisibleToConsultee { get; set; }
}
