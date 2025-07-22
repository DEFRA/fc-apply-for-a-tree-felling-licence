namespace Forestry.Flo.Services.FileStorage.ResultModels;

public class StoreFileFailureResult
{
    public FileInvalidReason InvalidReason { get; }

    public StoreFileFailureResultReason StoreFileFailureResultReason { get; }

    public static StoreFileFailureResult CreateWithInvalidFileReason(FileInvalidReason invalidFileReason)
    {
        return new StoreFileFailureResult(StoreFileFailureResultReason.FailedValidation, invalidFileReason);
    }

    private StoreFileFailureResult(StoreFileFailureResultReason storeFileFailureResultReason, 
        FileInvalidReason invalidFileReason)
    {
        InvalidReason = invalidFileReason;

        StoreFileFailureResultReason = storeFileFailureResultReason;
    }

    public StoreFileFailureResult(StoreFileFailureResultReason storeFileFailureResultReason)
    {
        StoreFileFailureResultReason = storeFileFailureResultReason;
    }

    protected StoreFileFailureResult()
    {
        
    }
}