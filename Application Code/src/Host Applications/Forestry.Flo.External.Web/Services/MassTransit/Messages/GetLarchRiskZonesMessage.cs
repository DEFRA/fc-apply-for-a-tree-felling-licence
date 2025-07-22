using Forestry.Flo.External.Web.Services.MassTransit.Consumers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.External.Web.Services.MassTransit.Messages;

/// <summary>
/// Represents a message to retrieve Larch Risk Zones for specified compartments.
/// </summary>
public class GetLarchRiskZonesMessage
{
    /// <summary>
    /// Creates a new instance of a <see cref="GetLarchRiskZonesMessage"/>.
    /// </summary>
    /// <param name="compartmentIds">The identifiers for the compartments to retrieve Larch Risk Zones for.</param>
    /// <param name="userId">The identifier for the user associated with the request.</param>
    /// <param name="applicationId">The identifier for the application associated with the request.</param>
    public GetLarchRiskZonesMessage(IEnumerable<Guid> compartmentIds, Guid userId, Guid applicationId)
    {
        CompartmentIds = compartmentIds;
        UserId = userId;
        ApplicationId = applicationId;
    }

    /// <summary>
    /// Gets and inits the identifiers for the compartments to retrieve Larch Risk Zones for.
    /// </summary>
    public IEnumerable<Guid> CompartmentIds { get; init; }

    /// <summary>
    /// Gets and inits the identifier for the user associated with the request.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets and inits the identifier for the application associated with the request.
    /// </summary>
    public Guid ApplicationId { get; init; }
}
