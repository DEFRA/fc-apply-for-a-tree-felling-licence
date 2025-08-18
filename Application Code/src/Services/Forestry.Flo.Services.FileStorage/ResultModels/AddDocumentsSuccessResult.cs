namespace Forestry.Flo.Services.FileStorage.ResultModels;

/// <summary>
/// Success result model for adding documents to a felling licence application.
/// </summary>
public class AddDocumentsSuccessResult
{
    public readonly IEnumerable<Guid> DocumentIds;
    public readonly IEnumerable<string> UserFacingFailureMessages;

    /// <summary>
    /// Creates a new instance of <see cref="AddDocumentsSuccessResult"/>.
    /// </summary>
    /// <param name="documentIds">A list of document ids of the newly-attached documents.</param>
    /// <param name="userFacingFailureMessages">A list of any error messages to return to the user.</param>
    public AddDocumentsSuccessResult(
        IEnumerable<Guid> documentIds,
        IEnumerable<string> userFacingFailureMessages)
    {
        DocumentIds = documentIds;
        UserFacingFailureMessages = userFacingFailureMessages;
    }
}