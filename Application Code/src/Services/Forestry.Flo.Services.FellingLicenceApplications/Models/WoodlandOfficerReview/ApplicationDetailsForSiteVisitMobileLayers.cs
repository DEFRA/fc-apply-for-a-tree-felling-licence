using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

public class ApplicationDetailsForSiteVisitMobileLayers
{
    public string CaseReference { get; set; }

    public List<InternalFullCompartmentDetails> Compartments { get; set; }
}