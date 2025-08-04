namespace MigrationHostApp.Validation;

public class PreValidationResultError
{
    public ICollection<ValidationResultFailure> ValidationResultFailures = new List<ValidationResultFailure>();
    public string? ExceptionMessage;
}