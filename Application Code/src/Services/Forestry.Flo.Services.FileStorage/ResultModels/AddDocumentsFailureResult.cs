namespace Forestry.Flo.Services.FileStorage.ResultModels;

public class AddDocumentsFailureResult
{
    public readonly IEnumerable<string> UserFacingFailureMessages;

    public AddDocumentsFailureResult(IEnumerable<string> userFacingFailureMessages)
    {
        UserFacingFailureMessages = userFacingFailureMessages;
    }
}