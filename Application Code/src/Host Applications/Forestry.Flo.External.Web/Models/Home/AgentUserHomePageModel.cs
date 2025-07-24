using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.WoodlandOwner;


namespace Forestry.Flo.External.Web.Models.Home;

public class AgentUserHomePageModel
{
    //TODO - entity class for FLAs
    /// <summary>
    /// Gets the collection of woodland owners an agent can work on behalf of.
    /// </summary>
    public IReadOnlyCollection<WoodlandOwnerSummary> WoodlandOwners { get; }

    public Guid AgencyId { get; }

    public AgentUserHomePageModel(
        Maybe<IReadOnlyCollection<WoodlandOwnerSummary>> woodlandOwners, Guid agencyId)
    {
        Guard.Against.Null(woodlandOwners);
        WoodlandOwners = woodlandOwners.HasValue
            ? woodlandOwners.Value
            : new List<WoodlandOwnerSummary>(0);
        AgencyId = agencyId;
    }
}