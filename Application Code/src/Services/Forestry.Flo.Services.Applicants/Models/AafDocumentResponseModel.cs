namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing a document file making up all or part of an uploaded AAF.
/// </summary>
public class AafDocumentResponseModel
{
    /// <summary>
    /// Gets and inits the name of the file.
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Gets and inits the size of the file.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Gets and inits the type of the file.
    /// </summary>
    public string FileType { get; init; }

    /// <summary>
    /// Gets and inits the content type of the document.
    /// </summary>
    public string MimeType { get; init; }

    /// <summary>
    /// Gets and inits the location of this document.
    /// </summary>
    public string Location { get; init; }
}