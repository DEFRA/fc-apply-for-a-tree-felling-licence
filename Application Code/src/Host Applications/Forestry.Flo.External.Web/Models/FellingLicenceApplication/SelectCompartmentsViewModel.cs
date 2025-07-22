using Forestry.Flo.External.Web.Models.Compartment;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class SelectCompartmentsViewModel
{
    public FellingLicenceApplicationModel Application { get; set; } = null!;
    public IReadOnlyCollection<CompartmentModel> Compartments { get; set; } = null!;
}