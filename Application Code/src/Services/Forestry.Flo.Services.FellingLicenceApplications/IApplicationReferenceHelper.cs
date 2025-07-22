using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications;

public interface IApplicationReferenceHelper
{
    /// <summary>
    /// Generates a reference number for the felling licence application.
    /// </summary>
    /// <param name="application">The application details to use</param>
    /// <param name="counter">The unqiue ID for the record</param>
    /// <param name="postFix">Optional postfix to add to the end of the string</param>
    /// <param name="startingOffset">The value to add to <see cref="counter"/></param>
    /// <returns>A formated strind</returns>
    string GenerateReferenceNumber(FellingLicenceApplication application, long counter, string? postFix, int? startingOffset = 0);

    /// <summary>
    /// Updates the prefix of the reference number.
    /// <param name="reference">The original reference number.</param>
    /// <param name="prefix">The new prefix to set.</param>
    /// <returns>The updated reference number.</returns>
    string UpdateReferenceNumber(string reference, string prefix);
}