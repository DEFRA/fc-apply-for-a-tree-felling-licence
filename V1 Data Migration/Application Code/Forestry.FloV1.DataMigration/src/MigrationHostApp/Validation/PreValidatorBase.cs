using System.Collections.Concurrent;

namespace MigrationHostApp.Validation;

public class PreValidatorBase
{
    protected readonly ConcurrentBag<ValidationResultFailure> ValidationResultFailures;

    protected PreValidatorBase()
    {
        ValidationResultFailures = new ConcurrentBag<ValidationResultFailure>();
    }

    protected void CheckForMissingData(long entityId, string fieldName, string? dbValue, string? message=null)
    {
        if (string.IsNullOrEmpty(dbValue))
        {
            ValidationResultFailures.Add(new ValidationResultFailure
            {
                RowId = entityId,
                ItemValidationIssue = DataItemValidationIssue.DataMissing,
                FieldName = fieldName,
                Message = message
            });
        }
    }
}