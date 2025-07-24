using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class ConfirmedFellingSpeciesModel
{
    [HiddenInput]
    public Guid? Id { get; set; }

    public string? Species { get; set; }

    public SpeciesType? SpeciesType { get; set; }

    public bool Deleted { get; set; } = false;
}