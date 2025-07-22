using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing legacy documents held in storage.
/// </summary>
public class LegacyDocumentModel
{
    /// <summary>
    /// Gets and inits the id of the legacy document.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets and inits the id of the woodland owner that owns this legacy document.
    /// </summary>
    public Guid WoodlandOwnerId { get; init; }

    /// <summary>
    /// Gets and inits the name of the woodland owner that owns this legacy document.
    /// </summary>
    public string WoodlandOwnerName { get; init; }

    /// <summary>
    /// Gets and inits the <see cref="LegacyDocumentType"/> for this legacy document.
    /// </summary>
    public LegacyDocumentType DocumentType { get; init; }

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