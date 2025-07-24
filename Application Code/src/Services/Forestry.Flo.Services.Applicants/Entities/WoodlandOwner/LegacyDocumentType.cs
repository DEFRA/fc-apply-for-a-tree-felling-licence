using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

/// <summary>
/// An enumeration of document types for <see cref="LegacyDocument"/> entities.
/// </summary>
public enum LegacyDocumentType
{
    [Display(Name = "Licence PDF")]
    LicencePdf,

    [Display(Name = "Other legacy document")]
    Other
}