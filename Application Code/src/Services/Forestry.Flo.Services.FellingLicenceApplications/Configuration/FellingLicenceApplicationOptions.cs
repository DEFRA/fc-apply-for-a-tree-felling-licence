namespace Forestry.Flo.Services.FellingLicenceApplications.Configuration;

public class FellingLicenceApplicationOptions
{
    public static string ConfigurationKey => "FellingLicenceApplication";

    /// <summary>
    /// Gets and sets the number of days following submission that an application's final action date should be set to.
    /// </summary>
    public int FinalActionDateDaysFromSubmission { get; set; }

    /// <summary>
    /// Gets and sets the length of time following receipt of an application to calculate the citizens charter date.
    /// </summary>
    public TimeSpan CitizensCharterDateLength { get; set; } = new(77, 0, 0, 0);

    /// <summary>
    /// Gets and sets the PostFix to add to the reference code        
    /// </summary>
    public string? PostFix { get; set; }

    /// <summary>
    /// The counter to use for the application reference number.
    /// </summary>
    public int? StartingOffset { get; set; } = 0;

    /// <summary>
    /// The Default License Duration in years
    /// </summary>
    public int DefaultLicenseDuration { get; set; } = 5;
}