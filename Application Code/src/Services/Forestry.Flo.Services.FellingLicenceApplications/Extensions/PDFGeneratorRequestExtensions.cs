using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Extensions;

public static class PdfGeneratorRequestExtensions
{
    public static void MakeSafeForPdfService(this PDFGeneratorRequest model)
    {
        foreach (var property in model.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(model,
                    ReplaceSpecialCharactersThatBreakThePdfService(property.GetValue(model) as string));
            }
        }
    }

    private static string ReplaceSpecialCharactersThatBreakThePdfService(string? value)
    {
        if (value == null)
            return string.Empty;

        return value
            .Replace("\r", string.Empty)
            .Replace("\t", "  ")
            .Replace("\b", string.Empty)
            .Replace("\f", string.Empty);
    }
}