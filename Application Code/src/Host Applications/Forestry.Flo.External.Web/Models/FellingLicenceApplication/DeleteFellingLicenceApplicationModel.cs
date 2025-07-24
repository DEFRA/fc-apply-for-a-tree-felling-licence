using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class DeleteFellingLicenceApplicationModel
{
    /// <summary>
    /// Gets or sets the Application id
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the Application reference
    /// </summary>
    public string? ApplicationReference { get; set; }
}