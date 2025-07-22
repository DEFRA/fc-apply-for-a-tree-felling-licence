namespace Forestry.Flo.Services.FileStorage.ResultModels;

public class AddDocumentsSuccessResult
{
    public readonly IEnumerable<string> UserFacingFailureMessages;

    public AddDocumentsSuccessResult(IEnumerable<string> userFacingFailureMessages)
    {
        UserFacingFailureMessages = userFacingFailureMessages;
    }
}