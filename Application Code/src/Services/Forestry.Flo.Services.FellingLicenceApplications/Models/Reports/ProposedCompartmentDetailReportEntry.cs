using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

public class ProposedCompartmentDetailReportEntry
{
    public ProposedDetailType ProposedDetailType { get; set; }
    public string FellingLicenceReference { get; set; }
    public string CompartmentName { get; set; }
    public string Species { get; set; }
    public double? PercentageOfRestockArea { get; set; }
    public double? Volume { get; set; }
    public FellingOperationType? OperationType { get; set; }
    public TypeOfProposal? RestockingProposal { get; set; }
    public double? Area { get; set; }
    public double? NumberOfTrees { get; set; }
    public string? NoRestockingReason { get; set; }
    public string? ProposedFellingReference { get; set; }
    public double? RestockingDensity { get; set; }
}