namespace Forestry.Flo.Internal.Web.Services
{
    public interface IAzureAdService
    {
        Task<bool> UserIsInDirectoryAsync(string emailAddress, CancellationToken cancellationToken = default);
    }
}
