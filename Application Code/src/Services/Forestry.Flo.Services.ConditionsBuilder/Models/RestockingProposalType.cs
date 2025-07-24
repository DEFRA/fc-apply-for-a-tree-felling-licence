namespace Forestry.Flo.Services.ConditionsBuilder.Models;

public enum RestockingProposalType
{
    None = 0,
    CreateDesignedOpenGround,
    DoNotIntendToRestock,
    PlantAnAlternativeArea,
    NaturalColonisation,
    PlantAnAlternativeAreaWithIndividualTrees,
    ReplantTheFelledArea,
    RestockByNaturalRegeneration,
    RestockWithCoppiceRegrowth,
    RestockWithIndividualTrees
}