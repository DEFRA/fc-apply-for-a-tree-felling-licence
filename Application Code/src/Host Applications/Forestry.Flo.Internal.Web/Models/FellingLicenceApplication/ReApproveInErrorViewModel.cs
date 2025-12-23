using Forestry.Flo.Services.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ReApproveInErrorViewModel : FellingLicenceApplicationPageViewModel
{
    [HiddenInput]
    public Guid Id { get; set; }

    public DateOnly? CurrentLicenceExpiryDate { get; set; }
    
    public DatePart? NewLicenceExpiryDate { get; set; }

    public string? CurrentSupplementaryPoints { get; set; }

    public ApprovedInErrorViewModel ApprovedInErrorViewModel { get; set; }

    public bool IsReadonly { get; set; }
}
