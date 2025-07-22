namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// An interface for classes that can be completed, such as admin officer or woodland officer reviews.
/// </summary>
public interface ICompletable
{
    /// <summary>
    /// Determines whether the overall task is completed.
    /// </summary>
    /// <returns>A bool indicating the task's completion status.</returns>
    public bool IsCompletable();
}