namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class combining both the agent authority forms and the associated woodland owner model.
/// </summary>
public class AgentAuthorityFormsWithWoodlandOwnerResponseModel
{
    /// <summary>
    /// Gets and sets the Agency Id
    /// </summary>
    public Guid AgencyId {get; set;}

    /// <summary>
    /// Gets and sets the AgentAuthorityFormResponseModels associated 
    /// </summary>
    public List<AgentAuthorityFormResponseModel> AgentAuthorityFormResponseModels { get; set; } = new(); 
    
    /// <summary>
    /// Gets and sets the Woodland Owner model
    /// </summary>
    public WoodlandOwnerModel WoodlandOwnerModel { get; set; }
}
