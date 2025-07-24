namespace Forestry.Flo.Services.FellingLicenceApplications.Models
{
    public class PDFGeneratorRequest
    {
        public string? templateName { get; set; }
        public string? watermarked { get; set; }
        public PDFGeneratorData? data { get; set; }
    }

    public class PDFGeneratorData
    {
        public string? version { get; set; }
        public string? date { get; set; }
        public string? fcContactName { get; set; }
        public string? fcContactAddress { get; set; }
        public string? approveDate { get; set; }
        public string? applicationRef { get; set; }
        public string? managementPlan { get; set; }
        public string? woodName { get; set; }
        public string? ownerNameWithTitle { get; set; }
        public string? ownerAddress { get; set; }
        public string? expiryDate { get; set; }
        public string? approverName { get; set; }
        public string? propertyName { get; set; }
        public string? localAuthority { get; set; }
        public IList<PDFGeneratorFellingDetails>? approvedFellingDetails { get; set; }
        public IList<string>? restockingConditions { get; set; }
        public IList<string>? operationsMaps { get; set; }
        public string? restockingAdvisoryDetails { get; set; }
        public IList<string>? restockingMaps { get; set; }
    }

    public class PDFGeneratorFellingDetails
    {
        public string? fellingSiteSubcompartment { get; set; }
        public string? typeOfOperation { get; set; }
        public string? markingOfTrees { get; set; }
        public string? digitisedArea { get; set; }
        public string? totalNumberOfTrees { get; set; }
        public string? estimatedVolume { get; set; }
        public string? species { get; set; }
    }
}
