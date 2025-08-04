namespace MigrationHostApp.Validation;

public class ValidationResultFailure
{
    public long? RowId { get; set; }
    public object ActualValue { get; set; }
    public string? FieldName { get; set; }
    public DataItemValidationIssue? ItemValidationIssue { get; set; }
    public string? Message { get; set; } = "";
    public bool IsItemIssue => !string.IsNullOrEmpty(FieldName) && ItemValidationIssue.HasValue;
}