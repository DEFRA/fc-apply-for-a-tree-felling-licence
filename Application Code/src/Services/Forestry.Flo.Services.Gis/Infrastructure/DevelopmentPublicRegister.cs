using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
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
        string caseRef,
        string propertyName,
        string caseType,
        string gridRef,
        string nearestTown,
        string localAdminArea,
        string adminRegion,
        DateTime publicRegisterStart,
        int period,
        double? broadLeafArea,
        double? coniferousArea,
        double? openGroundArea,
        double? totalArea,
        List<InternalCompartmentDetails<Polygon>> compartments,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to put application case reference {CaseReference} onto the public register", caseRef);
        logger.LogDebug("Property name: {PropertyName}", propertyName);
        logger.LogDebug("Case type: {CaseType}", caseType);
        logger.LogDebug("Grid ref: {gridRef}", gridRef);
        logger.LogDebug("Nearest town: {NearestTown}", nearestTown);
        logger.LogDebug("Local authority: {LocalAuthority}", localAdminArea);
        logger.LogDebug("Admin region: {AdminRegion}", adminRegion);
        logger.LogDebug("Public register start date: {StartDate}", publicRegisterStart);
        logger.LogDebug("Period: {Period}", period);
        logger.LogDebug("Broadleaf area: {BroadleafArea}", broadLeafArea);
        logger.LogDebug("Conifer area: {ConiferArea}", coniferousArea);
        logger.LogDebug("Open ground area: {OpenGroundArea}", openGroundArea);
        logger.LogDebug("Total area: {TotalArea}", totalArea);
        logger.LogDebug("Count of compartments: {CountOfCompartments}", compartments.Count);

        var esriId = int.Parse(caseRef.Split('/')[0]);
        logger.LogInformation("Returning ESRI Id {EsriId}", esriId);

        return Result.Success(esriId);
    }

    public async Task<Result> RemoveCaseFromConsultationRegisterAsync(int objectId, string caseReference, DateTime endDateOnPR,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to remove application with ESRI id {EsriId} from the public register", objectId);
        return Result.Success();
    }

    public async Task<Result> AddCaseToDecisionRegisterAsync(
        int objectId,
        string caseReference,
        string fellingLicenceOutcome,
        DateTime caseApprovalDateTime,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to add application with ESRI id {EsriId} to the approval public register", objectId);
        return Result.Success();
    }

    public async Task<Result> RemoveCaseFromDecisionRegisterAsync(int objectId, string caseReference, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development public register : received request to remove application with ESRI id {EsriId} from the approval public register", objectId);
        return Result.Success();
    }
}