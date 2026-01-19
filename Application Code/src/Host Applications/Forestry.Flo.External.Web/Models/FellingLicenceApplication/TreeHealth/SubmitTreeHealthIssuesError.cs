namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;

/// <summary>
/// Enumeration of possible errors when submitting tree health issues.
/// </summary>
public enum SubmitTreeHealthIssuesError
{
    /// <summary>
    /// An error occurred while uploading tree health issues documents.
    /// </summary>
    DocumentUpload,

    /// <summary>
    /// An error occurred while storing tree health issues.
    /// </summary>
    StoreTreeHealthIssues
}