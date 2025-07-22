using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class returned in response to a <see cref="GetAgentAuthorityFormRequest"/>.
/// </summary>
public class GetAgentAuthorityFormResponse
{
    /// <summary>
    /// Gets and sets the id of the <see cref="AgentAuthority"/> linking the
    /// agency and woodland owner in the request.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the details of the <see cref="AgentAuthorityForm"/> that is 
    /// valid at the current time, if one exists.
    /// </summary>
    public Maybe<AgentAuthorityFormDetailsModel> CurrentAgentAuthorityForm { get; set; }

    /// <summary>
    /// Gets and sets the details of the <see cref="AgentAuthorityForm"/> that was
    /// valid at the point in time specified in the request, if a time was provided
    /// and an AAF existed at that time.
    /// </summary>
    public Maybe<AgentAuthorityFormDetailsModel> SpecificTimestampAgentAuthorityForm { get; set; }
}

public record AgentAuthorityFormDetailsModel(
    Guid Id,
    DateTime ValidFromDate,
    DateTime? ValidToDate);