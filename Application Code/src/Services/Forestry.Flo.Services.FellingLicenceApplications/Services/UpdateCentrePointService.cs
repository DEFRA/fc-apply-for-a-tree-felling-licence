using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;

using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class UpdateCentrePointService : IUpdateCentrePoint
{
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceRepository;
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationService;

    public UpdateCentrePointService(
        IFellingLicenceApplicationExternalRepository fellingLicenceRepository,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService)
    {
        _fellingLicenceRepository = Guard.Against.Null(fellingLicenceRepository);
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateCentrePointAsync(
        Guid applicationId,
       UserAccessModel userAccessModel,
        string areaCode,
        string administrativeRegion,
        string centrePoint,
        string osGridReference,
        CancellationToken cancellationToken)
    {
        var fellingLicence =
            await _getFellingLicenceApplicationService.GetApplicationByIdAsync(
                applicationId,
                userAccessModel,
                cancellationToken);

        if (fellingLicence.IsFailure)
        {
            return Result.Failure($"Unable to retrieve submitted property detail entity for application {applicationId}");
        }

        fellingLicence.Value.AreaCode = areaCode;
        fellingLicence.Value.CentrePoint = centrePoint;
        fellingLicence.Value.OSGridReference = osGridReference;
        fellingLicence.Value.AdministrativeRegion = administrativeRegion;

        var updateResult = await _fellingLicenceRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return updateResult.IsSuccess
            ? Result.Success()
            : Result.Failure(updateResult.Error.ToString());
    }
}