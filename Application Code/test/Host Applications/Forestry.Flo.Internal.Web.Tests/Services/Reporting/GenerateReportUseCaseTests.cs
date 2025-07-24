using System.Security.Claims;
using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.Reports;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Reports;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using Moq;
using NodaTime;
using IClock = NodaTime.IClock;

namespace Forestry.Flo.Internal.Web.Tests.Services.Reporting;

public class GenerateReportUseCaseTests
{
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _flaRepository = new();
    private readonly Mock<IAdminHubService> _adminHubService = new();
    private readonly Mock<IReportQueryService> _reportQueryService = new();
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Mock<IAuditService<GenerateReportUseCase>> _auditingService = new();
    private readonly Guid _requestContextUserId = Guid.NewGuid();
    private readonly Mock<IClock> _clock = new();
    private RequestContext _requestContext;
    private ClaimsPrincipal _principal;
    private InternalUser _user;
    private readonly DateTime _now = DateTime.Now.ToUniversalTime();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task GenerateReport_WhenDataFound_ReturnsFileStreamActionResult(
        ReportRequestViewModel viewModel,
        FellingLicenceApplicationsReportQueryResultModel data)
    {
        _reportQueryService.Setup(x =>
                x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>() ,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        viewModel.ToDay = "1";
        viewModel.ToMonth = "11";
        viewModel.ToYear = "2023";
        viewModel.FromDay = "1";
        viewModel.FromMonth = "12";
        viewModel.FromYear = "2023";

        var sut = CreateSut();
        
        var result = await sut.GenerateReportAsync(viewModel, _user, It.IsAny<CancellationToken>());
        Assert.True(result.IsSuccess);
        Assert.IsType<FileStreamResult>(result.Value);
        var fs = result.Value as FileStreamResult;
        Assert.Equal($"flo-export-{_now:yyyy-MMM-dd_HHmmss}.zip", fs.FileDownloadName);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ReportExecution
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _requestContextUserId
                && a.SourceEntityId == Guid.Empty
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    ZipArchiveFileName = fs.FileDownloadName,
                    MainFlaDataSetRowCount = data.FellingLicenceApplicationReportEntries.Count,
                    QueryModel = viewModel.ToQuery().Value,
                    
                }, _serializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _reportQueryService.Verify(x => x.QueryFellingLicenceApplicationsAsync(
            It.IsAny<FellingLicenceApplicationsReportQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task Generate_Report_WhenNoDataFound_ReturnsEmptyActionResult(ReportRequestViewModel viewModel)
    {
        _reportQueryService.Setup(x =>
                x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FellingLicenceApplicationsReportQueryResultModel() );

        viewModel.ToDay = "1";
        viewModel.ToMonth = "11";
        viewModel.ToYear = "2023";
        viewModel.FromDay = "1";
        viewModel.FromMonth = "12";
        viewModel.FromYear = "2023";

        var sut = CreateSut();

        var result = await sut.GenerateReportAsync(viewModel, _user, It.IsAny<CancellationToken>());
        Assert.True(result.IsSuccess);
        Assert.IsType<EmptyResult>(result.Value);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ReportExecutionNoDataFound
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _requestContextUserId
                && a.SourceEntityId == Guid.Empty
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    QueryModel = viewModel.ToQuery().Value,
                }, _serializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        _reportQueryService.Verify(x => x.QueryFellingLicenceApplicationsAsync(
            It.IsAny<FellingLicenceApplicationsReportQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);

          _reportQueryService.VerifyNoOtherCalls();

        _auditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task Generate_Report_WithInvalidViewModel_ReturnsFailureResult(ReportRequestViewModel viewModel, FellingLicenceApplicationsReportQueryResultModel data)
    {
        _reportQueryService.Setup(x =>
                x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var sut = CreateSut();

        var result = await sut.GenerateReportAsync(viewModel, _user, It.IsAny<CancellationToken>());

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to create report.", result.Error);
        
        _reportQueryService.Verify(x=>x.QueryFellingLicenceApplicationsAsync(
            It.IsAny<FellingLicenceApplicationsReportQuery>(), It.IsAny<CancellationToken>()),
            Times.Never());

        _auditingService.Verify(x=>x.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory, AutoData]
    public async Task Generate_Report_WhenReportQueryingFails_ReturnsFailureResult(ReportRequestViewModel viewModel)
    {
        _reportQueryService.Setup(x =>
                x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApplicationException("uh oh"));

        viewModel.ToDay = "1";
        viewModel.ToMonth = "11";
        viewModel.ToYear = "2023";
        viewModel.FromDay = "1";
        viewModel.FromMonth = "12";
        viewModel.FromYear = "2023";

        var sut = CreateSut();

        var result = await sut.GenerateReportAsync(viewModel, _user, It.IsAny<CancellationToken>());
        Assert.True(result.IsFailure);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ReportExecutionFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _requestContextUserId
                && a.SourceEntityId == Guid.Empty
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Error during execution of report."
                }, _serializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        _reportQueryService.Verify(x => x.QueryFellingLicenceApplicationsAsync(
            It.IsAny<FellingLicenceApplicationsReportQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _reportQueryService.VerifyNoOtherCalls();

        _auditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task Generate_Report_WhenNullUser_ReturnsWithError(ReportRequestViewModel viewModel)
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.GenerateReportAsync(viewModel, null, CancellationToken.None));

        _reportQueryService.Verify(x => x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory, AutoMoqData]
    public async Task GetReferenceModel_WhenValidUser_ReturnsSuccessWithViewModel(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels, 
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);
        
        var result = await sut.GetReferenceModelAsync(_user, false, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value.SelectedAdminHubIds);
        Assert.Empty(result.Value.SelectedConfirmedFellingOperationTypes);
        Assert.Empty(result.Value.SelectedConfirmedFellingSpecies);
        Assert.Empty(result.Value.SelectedProposedFellingOperationTypes);
        Assert.Null(result.Value.CurrentStatus);
        Assert.Null(result.Value.SelectedAdminOfficerId);
        Assert.Null(result.Value.SelectedWoodlandOfficerId);
        Assert.Equal(DateRangeTypeForReporting.SubmittedDate, result.Value.DateRangeType);

        _adminHubService.Verify(x=>x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once);

        _adminHubService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task GetReferenceModel_WhenNullUser_ReturnsFailure()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.GetReferenceModelAsync(null, addDefaultDates:true, CancellationToken.None));

        _reportQueryService.Verify(x => x.QueryFellingLicenceApplicationsAsync(It.IsAny<FellingLicenceApplicationsReportQuery>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory, AutoMoqData]
    public async Task GetReferenceModel_PopulatesModelWithAdminHubs(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels,
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);
        
        var result = await sut.GetReferenceModelAsync(_user, false, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);

        Assert.Equal(expectedAdminHubModels.Count, result.Value.AdminHubs.Count);

        foreach (var expectedAdminHubModel in expectedAdminHubModels)
        {
            var actualAdminHub = result.Value.AdminHubs.Single(x => x.Id == expectedAdminHubModel.Id);
            Assert.Equal(actualAdminHub.Name, expectedAdminHubModel.Name);
            Assert.Equal(actualAdminHub.AdminManagerUserAccountId, expectedAdminHubModel.AdminManagerUserAccountId);
            Assert.Equal(actualAdminHub.Areas.Count, expectedAdminHubModel.Areas.Count);
        }

        _adminHubService.Verify(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once);

        _adminHubService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetReferenceModel_PopulatesModelWithConfirmedFcUsers(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels,
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);
        
        var result = await sut.GetReferenceModelAsync(_user, false, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);

        Assert.Equal(expectedUserAccounts.Count, result.Value.ConfirmedFcUsers.Count);
        foreach (var expectedUserAccount in expectedUserAccounts)
        {
            var actualUser = result.Value.ConfirmedFcUsers.Single(x => x.Email == expectedUserAccount.Email);
            Assert.Equal(expectedUserAccount.FullName(false), actualUser.FullName);
            Assert.Equal(expectedUserAccount.AccountType.ToString(), actualUser.AccountType);
        }

        _adminHubService.Verify(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once);

        _adminHubService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task GetReferenceModel_WhenSpecified_AddsDefaultDates(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels,
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);

        var result = await sut.GetReferenceModelAsync(_user, true, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);

        var expectedToDate = DateTime.UtcNow;
        var expectedFromDate = expectedToDate.FirstDayOfWeek();

        Assert.Equal(expectedFromDate.Day.ToString(), result.Value.FromDay);
        Assert.Equal(expectedFromDate.Month.ToString(), result.Value.FromMonth);
        Assert.Equal(expectedFromDate.Year.ToString(), result.Value.FromYear);

        Assert.Equal(expectedToDate.Day.ToString(), result.Value.ToDay);
        Assert.Equal(expectedToDate.Month.ToString(), result.Value.ToMonth);
        Assert.Equal(expectedToDate.Year.ToString(), result.Value.ToYear);

        Assert.Equal(expectedFromDate.Date, result.Value.FromDateTime!.Value.Date);
        Assert.Equal(expectedToDate.Date, result.Value.ToDateTime!.Value.Date);
    }

    [Theory, AutoMoqData]
    public async Task GetReferenceModel_WhenSpecified_DoesEmptyDates(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels,
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);

        var result = await sut.GetReferenceModelAsync(_user, false, It.IsAny<CancellationToken>());

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value.FromDay);
        Assert.Null(result.Value.FromMonth);
        Assert.Null(result.Value.FromYear);
        Assert.Null(result.Value.ToDay);
        Assert.Null(result.Value.ToMonth);
        Assert.Null(result.Value.ToYear);
    }

    [Theory, AutoMoqData]
    public async Task GetReferenceModel_WhenUnspecified_DoesEmptyDates(
        IReadOnlyCollection<AdminHubModel> expectedAdminHubModels,
        List<UserAccount> expectedUserAccounts)
    {
        var sut = CreateSut();
        SetHappyPathMocksForGetReferenceModel(expectedAdminHubModels, expectedUserAccounts);

        var result = await sut.GetReferenceModelAsync(_user);

        Assert.True(result.IsSuccess);
        
        Assert.Null(result.Value.FromDay);
        Assert.Null(result.Value.FromMonth);
        Assert.Null(result.Value.FromYear);
        Assert.Null(result.Value.ToDay);
        Assert.Null(result.Value.ToMonth);
        Assert.Null(result.Value.ToYear);
    }

    [Fact]
    public async Task GetReferenceModel_WhenCannotQueryForAdminHubs_ReturnsFailureResult()
    {
        _adminHubService.Setup(x =>
                x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized));

        var sut = CreateSut();
        var result = await sut.GetReferenceModelAsync(_user);

        Assert.True(result.IsFailure);
    }

    private GenerateReportUseCase CreateSut()
    {
        ResetMocks();

        return new GenerateReportUseCase(
            _reportQueryService.Object,
            _adminHubService.Object,
            _internalUserAccountService.Object,
            _auditingService.Object,
            _clock.Object, 
            _requestContext,
            new RecyclableMemoryStreamManager(),
            new NullLogger<GenerateReportUseCase>());
    }

    private void ResetMocks()
    {
        _principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: _requestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
       
        _requestContext = new RequestContext(
            _requestContextCorrelationId,
            new RequestUserModel(_principal));

        _user = new InternalUser(_principal);

        _auditingService.Reset();
        _internalUserAccountService.Reset();
        _flaRepository.Reset();
        _clock.Reset();
        _clock.Setup(x => x.GetCurrentInstant()).Returns( Instant.FromDateTimeUtc(_now));
    }

    private void SetHappyPathMocksForGetReferenceModel(IReadOnlyCollection<AdminHubModel> expectedAdminHubModels, List<UserAccount> expectedUserAccounts)
    {
        _adminHubService.Setup(x =>
                x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(expectedAdminHubModels));

        _internalUserAccountService.Setup(x =>
                x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(expectedUserAccounts);
    }
}
