using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AgentAuthorityFormViewModel
{
    public bool CouldRetrieveAgentAuthorityFormDetails { get; set; }

    /// <summary>
    /// Gets and sets an optional URI for agent authority management on the external application.
    /// </summary>
    public Maybe<string> AgentAuthorityFormManagementUrl { get; set; }

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

    /// <summary>
    /// Gets a bool indicating that the specific and current AAFs are different, or there
    /// is a specific AAF but no current AAF.
    /// </summary>
    public bool SpecificAafIsNotCurrent => SpecificTimestampAgentAuthorityForm.HasValue
                                           && ((CurrentAgentAuthorityForm.HasValue
                                                && SpecificTimestampAgentAuthorityForm.Value.Id != CurrentAgentAuthorityForm.Value.Id)
                                               || CurrentAgentAuthorityForm.HasNoValue);
}