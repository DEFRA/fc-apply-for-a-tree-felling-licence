using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class RemoveFromPublicRegisterModel
{
    [HiddenInput]
    public string ApplicationReference { get; set; }

    [HiddenInput]
    public int? EsriId { get; set; }
}