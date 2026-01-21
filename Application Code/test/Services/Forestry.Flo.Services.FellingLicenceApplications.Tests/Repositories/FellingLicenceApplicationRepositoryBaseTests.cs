using AutoFixture;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public class FellingLicenceApplicationRepositoryBaseTests
{
    private readonly ExternalUserContextFlaRepository _sut;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private readonly Mock<IApplicationReferenceHelper> _referenceGenerator;
    private readonly Mock<IFellingLicenceApplicationReferenceRepository> _mockReferenceRepository;
    private readonly Fixture _fixture = new();
    
    public FellingLicenceApplicationRepositoryBaseTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _referenceGenerator = new Mock<IApplicationReferenceHelper>();
        _referenceGenerator.Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("test");
        _mockReferenceRepository = new Mock<IFellingLicenceApplicationReferenceRepository>();
        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _sut = new ExternalUserContextFlaRepository(_fellingLicenceApplicationsContext, _referenceGenerator.Object, _mockReferenceRepository.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnTrue_GivenApplicationExists(FellingLicenceApplication licenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.Add(licenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        //act
        var result = await _sut.CheckApplicationExists(licenceApplication.Id, CancellationToken.None);

        //assert
        Assert.True(result);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFalse_GivenApplicationDoesNotExist(FellingLicenceApplication licenceApplication)
    {
        //act
        var result = await _sut.CheckApplicationExists(licenceApplication.Id, CancellationToken.None);

        //assert
        Assert.False(result);
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddDocument_GivenExistingApplication(FellingLicenceApplication licenceApplication, Document document)
    {
        //arrange
        
        licenceApplication.Documents!.Clear();
        _fellingLicenceApplicationsContext.Add(licenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        document.FellingLicenceApplicationId = licenceApplication.Id;
        document.FellingLicenceApplication = licenceApplication;

        //act
        var result = await _sut.AddDocumentAsync(document, CancellationToken.None);

        var updatedFla = await _sut.GetAsync(licenceApplication.Id, CancellationToken.None);
        //assert
        Assert.True(result.IsSuccess);
        Assert.Single(updatedFla.Value.Documents!);
        Assert.Equal(document, updatedFla.Value.Documents[0]);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateStepStatuses_GivenValuesChanged(FellingLicenceApplication fla, ApplicationStepStatusRecord stepStatusRecord)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var compartmentStatusList =
            fla.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses!.Select(x =>
                new CompartmentFellingRestockingStatus
                {
                    CompartmentId = x.CompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                }).ToList();

        stepStatusRecord = stepStatusRecord with
        {
            FellingAndRestockingDetailsComplete = compartmentStatusList
        };

        var result = await _sut.UpdateApplicationStepStatusAsync(fla.Id, stepStatusRecord, CancellationToken.None)
            .ConfigureAwait(false);

        var updatedFla = await _sut.GetAsync(fla.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert step statuses have been set accordingly

        Assert.Equal(stepStatusRecord.TermsAndConditionsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.TermsAndConditionsStatus);
        Assert.Equal(stepStatusRecord.ConstraintsCheckComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);
        Assert.Equal(stepStatusRecord.OperationDetailsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.OperationsStatus);
        Assert.Equal(stepStatusRecord.SelectedCompartmentsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus);
        Assert.Equal(stepStatusRecord.SupportingDocumentationComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus);
        Assert.Equal(stepStatusRecord.TreeHealthComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus);
        if (stepStatusRecord.PawsCheckComplete is true)
        {
            Assert.All(updatedFla.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses, x => Assert.False(x.Status));
        }

        Assert.Equivalent(stepStatusRecord.FellingAndRestockingDetailsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateStepStatuses_GivenSomeValuesChanged(FellingLicenceApplication fla, ApplicationStepStatusRecord stepStatusRecord)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        stepStatusRecord = stepStatusRecord with
        {
            ConstraintsCheckComplete = null,
            SupportingDocumentationComplete = null
        };

        var compartmentStatusList =
            fla.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses!.Select(x =>
                new CompartmentFellingRestockingStatus
                {
                    CompartmentId = x.CompartmentId,
                    FellingStatuses = new List<FellingStatus>(),
                }).ToList();

        stepStatusRecord = stepStatusRecord with
        {
            FellingAndRestockingDetailsComplete = compartmentStatusList
        };

        var result = await _sut.UpdateApplicationStepStatusAsync(fla.Id, stepStatusRecord, CancellationToken.None)
            .ConfigureAwait(false);

        var updatedFla = await _sut.GetAsync(fla.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert step statuses have been set accordingly

        Assert.Equal(stepStatusRecord.TermsAndConditionsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.TermsAndConditionsStatus);
        Assert.Equal(fla.FellingLicenceApplicationStepStatus.ConstraintCheckStatus, updatedFla.Value.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);
        Assert.Equal(stepStatusRecord.OperationDetailsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.OperationsStatus);
        Assert.Equal(stepStatusRecord.SelectedCompartmentsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus);
        Assert.Equal(fla.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus, updatedFla.Value.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus);
        Assert.Equivalent(stepStatusRecord.FellingAndRestockingDetailsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses);
        Assert.Equivalent(stepStatusRecord.TreeHealthComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotUpdateCompartmentCompletionStatus_GivenNoMatches(FellingLicenceApplication fla, ApplicationStepStatusRecord stepStatusRecord)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateApplicationStepStatusAsync(fla.Id, stepStatusRecord, CancellationToken.None)
            .ConfigureAwait(false);

        var updatedFla = await _sut.GetAsync(fla.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert step statuses have been set accordingly

        Assert.Equal(stepStatusRecord.TermsAndConditionsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.TermsAndConditionsStatus);
        Assert.Equal(fla.FellingLicenceApplicationStepStatus.ConstraintCheckStatus, updatedFla.Value.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);
        Assert.Equal(stepStatusRecord.OperationDetailsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.OperationsStatus);
        Assert.Equal(stepStatusRecord.SelectedCompartmentsComplete, updatedFla.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus);
        Assert.Equal(fla.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus, updatedFla.Value.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus);

        Assert.Equivalent(fla.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses, updatedFla.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses);
    }

    [Theory, AutoMoqData]
    public async Task CheckUserCanAccessApplication_WhenApplicationNotFound(
        Guid unknownApplicationId,
        UserAccessModel userAccessModel)
    {
        var result = await _sut.CheckUserCanAccessApplicationAsync(unknownApplicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task CheckUserCanAccessApplication_WhenUserIsFcUser(FellingLicenceApplication fla)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            UserAccountId = Guid.NewGuid(),
            IsFcUser = true
        };

        var result = await _sut.CheckUserCanAccessApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task CheckUserCanAccessApplication_WhenUserHasAccessToTheWoodlandOwnerId(FellingLicenceApplication fla)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
        };

        var result = await _sut.CheckUserCanAccessApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task CheckUserCanAccessApplication_WhenUserDoesNotHaveAccessToTheWoodlandOwnerId(
        FellingLicenceApplication fla,
        Guid anotherWoodlandOwnerId)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { anotherWoodlandOwnerId }
        };

        var result = await _sut.CheckUserCanAccessApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationReference_WhenApplicationIsFound(FellingLicenceApplication fla)
    {
        _fellingLicenceApplicationsContext.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetApplicationReferenceAsync(fla.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fla.ApplicationReference, result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationReference_WhenApplicationIsNotFound(Guid applicationId)
    {
        var result = await _sut.GetApplicationReferenceAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}