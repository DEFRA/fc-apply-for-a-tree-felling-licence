using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Gis.Infrastructure;

public class DevelopmentPublicRegister(ILogger<DevelopmentPublicRegister> logger) : IPublicRegister
{
    public async Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAsync(
        string caseReference,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request for comments for case reference {CaseReference}", caseReference);

        return Result.Success(new List<EsriCaseComments>() { new EsriCaseComments()
        {
            CaseNote = "New comment",
            Firstname = "John",
            Surname = "Smith",
            CreatedDate = DateTime.UtcNow,
            CaseReference = caseReference,
            GlobalID = Guid.Parse("9d029ff6-2a4e-4d25-92bc-976d385441b6")
        } });
    }

    public async Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAndDateAsync(
        string caseReference,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request for comments for case reference {CaseReference} and Date {Date}", caseReference, date);

        return Result.Success(new List<EsriCaseComments>() { new EsriCaseComments()
        {
            CaseNote = "New comment",
            Firstname = "John",
            Surname = "Smith",
            CreatedDate = DateTime.UtcNow,
            CaseReference = caseReference,
            GlobalID = Guid.Parse("9d029ff6-2a4e-4d25-92bc-976d385441b6")
        } });
    }

    public async Task<Result<int>> AddCaseToConsultationRegisterAsync(
        AddToPublicRegisterModel dataModel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to put application case reference {CaseReference} onto the public register", dataModel.CaseReference);
        logger.LogDebug("Property name: {PropertyName}", dataModel.PropertyName);
        logger.LogDebug("Case type: {CaseType}", dataModel.CaseType);
        logger.LogDebug("Grid ref: {gridRef}", dataModel.GridReference);
        logger.LogDebug("Nearest town: {NearestTown}", dataModel.NearestTown);
        logger.LogDebug("Local authority: {LocalAuthority}", dataModel.LocalAuthority);
        logger.LogDebug("Admin region: {AdminRegion}", dataModel.AdminRegion);
        logger.LogDebug("Public register start date: {StartDate}", dataModel.PublicRegisterStart);
        logger.LogDebug("Period: {Period}", dataModel.Period);
        logger.LogDebug("Total area: {TotalArea}", dataModel.TotalArea);
        logger.LogDebug("Count of compartments: {CountOfCompartments}", dataModel.Compartments.Count);

        var esriId = int.TryParse(dataModel.CaseReference.Split('/')[0], out var result) ? result : 1;
        logger.LogInformation("Returning ESRI Id {EsriId}", esriId);

        return Result.Success(esriId);
    }

    public async Task<Result> RemoveCaseFromConsultationRegisterAsync(int objectId, string caseReference, DateTime endDateOnPR,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to remove application with ESRI id {EsriId} from the public register", objectId);
        return Result.Success();
    }

    public async Task<Result<int>> AddCaseToDecisionRegisterAsync(
        AddToDecisionPublicRegisterModel dataModel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to add application with ESRI id {EsriId} to the approval public register", dataModel.ExistingEsriId);

        var esriId = int.TryParse(dataModel.CaseReference.Split('/')[0], out var result) ? result : 1;
        logger.LogInformation("Returning ESRI Id {EsriId}", esriId);

        return Result.Success(esriId);
    }

    public async Task<Result> RemoveCaseFromDecisionRegisterAsync(int objectId, string caseReference, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to remove application with ESRI id {EsriId} from the approval public register", objectId);
        return Result.Success();
    }
}