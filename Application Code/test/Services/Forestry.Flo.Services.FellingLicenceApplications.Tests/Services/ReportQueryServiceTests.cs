using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ReportQueryServiceTests
{
    private static readonly Fixture FixtureInstance = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IAdminHubService> _adminHubService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICompartmentRepository> _compartmentRepositoryService = new();

    public ReportQueryServiceTests()
    {
        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory, AutoMoqData]
    public async Task ShouldBuildQueryAndExecuteQuery(
      List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var dataSetUserIds = GetUserData(applications);

        EnsureKnownSpecies(applications);

        if (applications.NotAny(x=>x.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister))
        {
            applications.First().PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true;
        }

        _fellingLicenceApplicationRepository.Setup(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
             It.IsAny<FellingLicenceApplicationsReportQuery>(),
             It.IsAny<CancellationToken>())).ReturnsAsync(applications);

        var query = new FellingLicenceApplicationsReportQuery {};

        var result = await sut.QueryFellingLicenceApplicationsAsync(query, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);
        Assert.IsType<FellingLicenceApplicationsReportQueryResultModel>(result.Value);
        Assert.Equal(applications.Count, result.Value.FellingLicenceApplicationReportEntries.Count);
        Assert.NotEmpty(result.Value.ConfirmedCompartmentDetailReportEntries);
        Assert.Equal(applications.Count(x=>x.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister), result.Value.ConsultationPublicRegisterExemptCases.Count);
        Assert.NotEmpty(result.Value.ProposedCompartmentDetailReportEntries);
        Assert.NotEmpty(result.Value.SubmittedPropertyProfileReportEntries);
        Assert.True(result.Value.HasData);

        _fellingLicenceApplicationRepository.
            Verify(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
                    It.Is<FellingLicenceApplicationsReportQuery>(
                        y =>
                            y.CurrentStatus == query.CurrentStatus &&
                            y.AssociatedAdminHubUserIds == query.AssociatedAdminHubUserIds &&
                            y.CurrentStatus == query.CurrentStatus &&
                            y.ConfirmedFellingSpecies == query.ConfirmedFellingSpecies &&
                            y.DateRangeTypeForReport == query.DateRangeTypeForReport &&
                            y.SelectedAdminHubIds == query.SelectedAdminHubIds &&
                            y.ConfirmedFellingOperationTypes.OrderBy(x => x).SequenceEqual(query.ConfirmedFellingOperationTypes.OrderBy(x => x)) &&
                            y.DateFrom == query.DateFrom &&
                            y.DateTo == query.DateTo &&
                            y.ProposedFellingOperationTypes == query.ProposedFellingOperationTypes &&
                            y.SelectedAdminOfficerId == query.SelectedAdminOfficerId &&
                            y.SelectedWoodlandOfficerId == query.SelectedWoodlandOfficerId),
                    It.IsAny<CancellationToken>()
                )
            );

        _internalUserAccountService.Verify(x => x.RetrieveUserAccountsByIdsAsync(
                It.Is<List<Guid>>(y => y.OrderBy(x => x).SequenceEqual(dataSetUserIds.Distinct().ToList().OrderBy(x => x))),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _adminHubService.Verify(x => x.RetrieveAdminHubDataAsync(
                It.IsAny<GetAdminHubsDataRequestModel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _adminHubService.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task ShouldPopulateQueryWithAdminHubUserIdsIfAdminHubsInQuery(
        List<FellingLicenceApplication> applications, 
        IReadOnlyCollection<AdminHubModel> adminHubModels)
    {
        var sut = CreateSut();
         
         var dataSetUserIds = GetUserData(applications);
         var hubData = SetupAdminHubDataForTest(adminHubModels);
         var adminHubUserIds = hubData.Item2;

         EnsureKnownSpecies(applications);

        _fellingLicenceApplicationRepository.Setup(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
             It.IsAny<FellingLicenceApplicationsReportQuery>(),
             It.IsAny<CancellationToken>())).ReturnsAsync(applications);

         var query = new FellingLicenceApplicationsReportQuery { SelectedAdminHubIds = hubData.Item1 };

         var result = await sut.QueryFellingLicenceApplicationsAsync(query, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);
        Assert.IsType<FellingLicenceApplicationsReportQueryResultModel>(result.Value);
        Assert.Equal(applications.Count, result.Value.FellingLicenceApplicationReportEntries.Count);
        Assert.NotEmpty(result.Value.ConfirmedCompartmentDetailReportEntries);
        Assert.Equal(applications.Count(x => x.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister), result.Value.ConsultationPublicRegisterExemptCases.Count);
        Assert.NotEmpty(result.Value.ProposedCompartmentDetailReportEntries);
        Assert.NotEmpty(result.Value.SubmittedPropertyProfileReportEntries);
        Assert.True(result.Value.HasData);

        _fellingLicenceApplicationRepository.
             Verify(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
                     It.Is<FellingLicenceApplicationsReportQuery>(
                         y =>
                             y.CurrentStatus == query.CurrentStatus &&
                             y.AssociatedAdminHubUserIds.OrderBy(x => x).SequenceEqual(adminHubUserIds.OrderBy(x => x)) &&
                             y.CurrentStatus == query.CurrentStatus &&
                             y.ConfirmedFellingSpecies == query.ConfirmedFellingSpecies &&
                             y.DateRangeTypeForReport == query.DateRangeTypeForReport &&
                             y.SelectedAdminHubIds == query.SelectedAdminHubIds &&
                             y.ConfirmedFellingOperationTypes.OrderBy(x=>x).SequenceEqual(query.ConfirmedFellingOperationTypes.OrderBy(x=>x)) &&
                             y.DateFrom == query.DateFrom &&
                             y.DateTo == query.DateTo &&
                             y.ProposedFellingOperationTypes == query.ProposedFellingOperationTypes &&
                             y.SelectedAdminOfficerId == query.SelectedAdminOfficerId &&
                             y.SelectedWoodlandOfficerId == query.SelectedWoodlandOfficerId),
                     It.IsAny<CancellationToken>()
                 )
             );

         _internalUserAccountService.Verify(x=>x.RetrieveUserAccountsByIdsAsync(
                 It.Is<List<Guid>>(y => y.OrderBy(x=>x).SequenceEqual(dataSetUserIds.Distinct().ToList().OrderBy(x=>x))), 
                 It.IsAny<CancellationToken>()), 
             Times.Once);

         _adminHubService.Verify(x => x.RetrieveAdminHubDataAsync(
                 It.Is<GetAdminHubsDataRequestModel>(
                     y=> y.PerformingUserAccountType == AccountTypeInternal.AdminHubManager &&
                         y.PerformingUserId == Guid.Empty),
                 It.IsAny<CancellationToken>()),
             Times.Once);
            
         _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
         _internalUserAccountService.VerifyNoOtherCalls();
         _adminHubService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldBuildQueryAndExecuteQuery_WhenReturnsNoData()
    {
        var sut = CreateSut();
        
        _fellingLicenceApplicationRepository.Setup(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
             It.IsAny<FellingLicenceApplicationsReportQuery>(),
             It.IsAny<CancellationToken>())).ReturnsAsync(new List<FellingLicenceApplication>());

        var query = new FellingLicenceApplicationsReportQuery { };

        var result = await sut.QueryFellingLicenceApplicationsAsync(query, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);
        Assert.IsType<FellingLicenceApplicationsReportQueryResultModel>(result.Value);
        Assert.Empty(result.Value.ConfirmedCompartmentDetailReportEntries);
        Assert.Empty(result.Value.ConsultationPublicRegisterExemptCases);
        Assert.Empty(result.Value.FellingLicenceApplicationReportEntries);
        Assert.Empty(result.Value.ProposedCompartmentDetailReportEntries);
        Assert.Empty(result.Value.SubmittedPropertyProfileReportEntries);
        Assert.False(result.Value.HasData);

        _fellingLicenceApplicationRepository.
            Verify(x => x.ExecuteFellingLicenceApplicationsReportQueryAsync(
                    It.Is<FellingLicenceApplicationsReportQuery>(
                        y =>
                            y.CurrentStatus == query.CurrentStatus &&
                            y.AssociatedAdminHubUserIds == query.AssociatedAdminHubUserIds &&
                            y.CurrentStatus == query.CurrentStatus &&
                            y.ConfirmedFellingSpecies == query.ConfirmedFellingSpecies &&
                            y.DateRangeTypeForReport == query.DateRangeTypeForReport &&
                            y.SelectedAdminHubIds == query.SelectedAdminHubIds &&
                            y.ConfirmedFellingOperationTypes.OrderBy(x => x).SequenceEqual(query.ConfirmedFellingOperationTypes.OrderBy(x => x)) &&
                            y.DateFrom == query.DateFrom &&
                            y.DateTo == query.DateTo &&
                            y.ProposedFellingOperationTypes == query.ProposedFellingOperationTypes &&
                            y.SelectedAdminOfficerId == query.SelectedAdminOfficerId &&
                            y.SelectedWoodlandOfficerId == query.SelectedWoodlandOfficerId),
                    It.IsAny<CancellationToken>()
                )
            );

        _internalUserAccountService.Verify(x => x.RetrieveUserAccountsByIdsAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _adminHubService.Verify(x => x.RetrieveAdminHubDataAsync(
                It.IsAny<GetAdminHubsDataRequestModel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _adminHubService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WhenExceptionInService()
    {
        var sut = CreateSut();

        _adminHubService.Setup(x =>
                x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApplicationException("uh oh"));

        var query = new FellingLicenceApplicationsReportQuery { };

        var result = await sut.QueryFellingLicenceApplicationsAsync(query, It.IsAny<CancellationToken>());

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to successfully execute query / create result for report.", result.Error);
    }

    private static Result<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome> GetAdminHubsAsSuccess(IReadOnlyCollection<AdminHubModel> adminHubModels)
    {
       return Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubModels);
    }

    private static void EnsureKnownSpecies(List<FellingLicenceApplication> applications)
    {
        foreach (var fellingLicenceApplication in applications)
        {
            foreach (var submittedFlaPropertyCompartment in fellingLicenceApplication.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
            {
                foreach (var confirmedFellingDetail in submittedFlaPropertyCompartment.ConfirmedFellingDetails)
                {
                    foreach (var species in confirmedFellingDetail.ConfirmedFellingSpecies)
                    {
                        species.Species = "AR";
                    }
                    foreach (var species in confirmedFellingDetail.ConfirmedRestockingDetails.SelectMany(x => x.ConfirmedRestockingSpecies))
                    {
                        species.Species = "AR";
                    }
                }
            }

            foreach (var fellingDetail in fellingLicenceApplication.LinkedPropertyProfile.ProposedFellingDetails)
            {
                foreach (var fellingSpecies in fellingDetail.FellingSpecies)
                {
                    fellingSpecies.Species = "AR";
                }

                foreach (var restockingDetail in fellingDetail.ProposedRestockingDetails)
                {
                    foreach (var restockSpecies in restockingDetail.RestockingSpecies)
                    {
                        restockSpecies.Species = "AR";
                    }
                }
            }
        }
    }

    private Tuple<List<Guid>, List<Guid>> SetupAdminHubDataForTest(IReadOnlyCollection<AdminHubModel> adminHubModels)
    {
        _adminHubService.Setup(x =>
                x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetAdminHubsAsSuccess(adminHubModels));

        var adminHubUserIds = new List<Guid>();
        var adminHubIds = new List<Guid>();

        foreach (var adminHubModel in adminHubModels)
        {
            adminHubIds.Add(adminHubModel.Id);
            adminHubUserIds.AddRange(adminHubModel.AdminOfficers.Select(officer => officer.UserAccountId));
        }

        return new Tuple<List<Guid>, List<Guid>>(adminHubIds, adminHubUserIds);
    }

    private IEnumerable<Guid> GetUserData(List<FellingLicenceApplication> applications)
    {
        var dataSetUserIds = new List<Guid>();

        foreach (var fellingLicenceApplication in applications)
        {
            dataSetUserIds.AddRange(fellingLicenceApplication.AssigneeHistories.Select(x => x.AssignedUserId));
        }

        var internalUsersInDataSet = new List<UserAccountModel>(dataSetUserIds.Count);

        foreach (var dataSetUserId in dataSetUserIds)
        {
            var internalUser = FixtureInstance.Create<UserAccountModel>();
            internalUser.UserAccountId = dataSetUserId;
            internalUsersInDataSet.Add(internalUser);
        }
        _internalUserAccountService.Setup(x =>
                x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(internalUsersInDataSet));


        return dataSetUserIds;

    }

    private ReportQueryService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _compartmentRepositoryService.Reset();
        _adminHubService.Reset();
        _internalUserAccountService.Reset();

        return new ReportQueryService(
            _fellingLicenceApplicationRepository.Object,
            _internalUserAccountService.Object,
            _compartmentRepositoryService.Object,
            _adminHubService.Object,
            new FlaStatusDurationCalculator(_clock.Object),
            new NullLogger<ReportQueryService>()
        );
    }
}
