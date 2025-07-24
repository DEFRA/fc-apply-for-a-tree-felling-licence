namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Request class representing the details of the felling and restocking for an application
/// to use to calculate the conditions for the application.
/// </summary>
public class CalculateConditionsRequest
{
    /// <summary>
    /// Gets and sets the id of the felling licence application the request is for.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether or not the request is for draft conditions.
    /// </summary>
    /// <remarks>The conditions will only be stored in the database if this value is
    /// set to False.  Only the Internal user system should ever set this to false.</remarks>
    public bool IsDraft { get; set; } = true;

    /// <summary>
    /// Gets and sets the restocking operations in the application used to
    /// calculate the conditions.
    /// </summary>
    public List<RestockingOperationDetails> RestockingOperations { get; set; }
}