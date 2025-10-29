using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model representing the Agent Authority Form page in the felling licence application process.
/// This model handles the display and management of agent authority documentation that allows agents
/// to act on behalf of woodland owners.
/// </summary>
public class AgentAuthorityFormModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <inheritdoc />
    public override Guid ApplicationId { get; set; }

    /// <inheritdoc />
    public string? ApplicationReference { get; set; }

    /// <inheritdoc />
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets the name of this task to display in navigation and breadcrumbs.
    /// </summary>
    public string TaskName => "Agent Authority Form";

    /// <inheritdoc />
    public override bool? StepComplete { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the woodland owner's Agent Authority Form document.
    /// </summary>
    public Guid? WoodlandOwnerAafId { get; set; }

    /// <summary>
    /// Gets or sets the filename or identifier of the Agent Authority Form document.
    /// </summary>
    public string? AafDocument { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the agent authority relationship.
    /// </summary>
    public Guid? AgentAuthorityId { get; set; }
}