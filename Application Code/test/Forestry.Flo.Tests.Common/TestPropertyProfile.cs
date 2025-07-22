using AutoMapper;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Tests.Common;

public class TestPropertyProfile : PropertyProfile
{
    public TestPropertyProfile(PropertyProfile profile) : base(profile.Name, profile.OSGridReference,
        profile.NearestTown, profile.NameOfWood, profile.HasWoodlandManagementPlan, profile.WoodlandManagementPlanReference,
        profile.IsWoodlandCertificationScheme, profile.WoodlandCertificationSchemeReference, profile.WoodlandOwnerId,
        profile.Compartments)
    {
        Id = profile.Id;
    }
    public TestPropertyProfile(
        Guid id,
        string name,
        string? osGridReference,
        string? nearestTown, 
        string? nameOfWood, 
        bool hasWoodlandManagementPlan, 
        string? woodlandManagementPlanReference, 
        bool isWoodlandCertificationScheme, 
        string? woodlandCertificationSchemeReference, 
        Guid woodlandOwnerId, IEnumerable<Compartment> compartments) 
        : base(
            name, 
            osGridReference, 
            nearestTown, 
            nameOfWood, 
            hasWoodlandManagementPlan, 
            woodlandManagementPlanReference,
            isWoodlandCertificationScheme, 
            woodlandCertificationSchemeReference, 
            woodlandOwnerId,
            compartments)
    {
        Id = id;
    }
    
    public void SetName(string newName)
    {
        Name = newName;
    }
    
    public void SetId(Guid id)
    {
        Id = id;
    }
}