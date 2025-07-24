namespace Forestry.Flo.External.Web.Infrastructure;

public record ErrorDetails(ErrorTypes ErrorType, string? FieldName = default);

public enum ErrorTypes
{
    NotFound,
    Conflict,
    NotAuthorised,
    BadData,
    InternalError
}
