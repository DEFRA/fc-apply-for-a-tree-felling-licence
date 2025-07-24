using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public enum TypeOfProposal
    {
        [Display(Name = "Select a value")]
        None = 0, 
        [Display(Name = "Create designed open ground")]
        [Description("Create permanent open space within the bounds of an existing woodland, for example to create rides, glades, tracks or forest roads.  Open space must be appropriately scaled to the size of the woodland and provide benefits by improving its' structural diversity and overall condition, and by creating additional woodland habitats.")]
        CreateDesignedOpenGround,
        [Display(Name = "Do not intend to restock")]
        [Description("Do not intend to restock")]
        DoNotIntendToRestock,
        [Display(Name = "Plant an alternative area")]
        [Description("Establish new trees in a different agreed location to the felled area, by planting seeds, saplings, or young trees.")]
        PlantAnAlternativeArea,
        [Display(Name = "Natural Colonisation")]
        [Description("Establish tree cover on previously un-wooded sites using natural processes, such as natural seed fall, seed dispersal, or vegetative suckering.")]
        NaturalColonisation,
        [Display(Name = "Plant an alternative area with individual trees")]
        [Description("Plant seeds, saplings, or young trees to replace felled trees.")]
        PlantAnAlternativeAreaWithIndividualTrees,
        [Display(Name = "Replant the felled area")]
        [Description("Plant seeds, saplings, or young trees to replace individual trees or areas of felled woodland.")]
        ReplantTheFelledArea,
        [Display(Name = "Restock by natural regeneration")]
        [Description("Re-establish tree cover on previously wooded sites using natural processes, such as natural seed fall, seed dispersal, or vegetative suckering. If the natural regeneration fails to establish, replanting will likely be required.")]
        RestockByNaturalRegeneration,
        [Display(Name = " Restock with coppice regrowth")]
        [Description("Regeneration of woody stems from a coppice stool following coppicing. If the coppice regrowth fails to establish, replanting will likely be required.")]
        RestockWithCoppiceRegrowth,
        [Display(Name = " Restock with individual trees")]
        [Description("Plant seeds, saplings, or young trees to replace felled trees.")]
        RestockWithIndividualTrees
    }
}
