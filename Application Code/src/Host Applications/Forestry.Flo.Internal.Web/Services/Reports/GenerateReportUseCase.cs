using Ardalis.GuardClauses;
using Forestry.Flo.Internal.Web.Models.Reports;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using NodaTime;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.IO;

namespace Forestry.Flo.Internal.Web.Services.Reports;

public class GenerateReportUseCase
{
    private readonly IReportQueryService _reportQueryService;
    private readonly IAdminHubService _adminHubService;
    private readonly IUserAccountService _internalUserAccountService;
    private readonly ILogger<GenerateReportUseCase> _logger;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<GenerateReportUseCase> _auditService;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    private const string ZipFileNamePrefix = "flo-export";

    public GenerateReportUseCase(
        IReportQueryService reportQueryService,
        IAdminHubService adminHubService,
        IUserAccountService internalUserAccountService,
        IAuditService<GenerateReportUseCase> auditService,
        IClock clock,
        RequestContext requestContext,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILogger<GenerateReportUseCase> logger)
    {
        _reportQueryService = Guard.Against.Null(reportQueryService);
        _internalUserAccountService = Guard.Against.Null(internalUserAccountService);
        _adminHubService = Guard.Against.Null(adminHubService);
        _clock = Guard.Against.Null(clock);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _logger = logger;
        _recyclableMemoryStreamManager = Guard.Against.Null(recyclableMemoryStreamManager);
    }

    public async Task<Result<IActionResult>> GenerateReportAsync(
        ReportRequestViewModel viewModel,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var getQueryModel = viewModel.ToQuery();

        if (getQueryModel.IsFailure)
        {
            _logger.LogWarning("Could not create query model from view model for report. Failure error: [{getQueryModelError}]", getQueryModel.Error);
            return Result.Failure<IActionResult>("Unable to create report.");
        }

        try
        {
            var executeQueryResult = await _reportQueryService.QueryFellingLicenceApplicationsAsync(getQueryModel.Value, cancellationToken);

            return executeQueryResult.IsSuccess
                ? await HandleSuccess(user, executeQueryResult.Value, getQueryModel.Value, cancellationToken)
                : await HandleFailure(user, executeQueryResult.Error, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Error during execution of report.");
            return await HandleFailure(user, "Error during execution of report.", cancellationToken);
        }
    }

    public async Task<Result<ReportRequestViewModel>> GetReferenceModelAsync(InternalUser user, bool addDefaultDates = false, CancellationToken cancellationToken = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var currentHubs = await _adminHubService.RetrieveAdminHubDataAsync(
            new GetAdminHubsDataRequestModel(user.UserAccountId!.Value, AccountTypeInternal.AdminHubManager), cancellationToken);

        if (currentHubs.IsFailure)
        {
            _logger.LogError("Failed to retrieve admin Hubs with error {Error}", currentHubs.Error);
            return Result.Failure<ReportRequestViewModel>(currentHubs.Error.ToString());
        }

        var fcUsers = await _internalUserAccountService.ListConfirmedUserAccountsAsync(cancellationToken);

        var users = fcUsers.OrderBy(x => x.LastName)
            .Select(userAccount => new ConfirmedFcUserModelForReporting
            {
                FullName = userAccount.FullName(includeTitle: false),
                Id = userAccount.Id,
                Email = userAccount.Email,
                AccountType = userAccount.AccountType.ToString()
            })
            .ToList();

        var model = new ReportRequestViewModel
        {
            AdminHubs = currentHubs.Value.ToList(),
            ConfirmedFcUsers = users
        };

        if (addDefaultDates)
        {
            var firstDayOfWeek = DateTime.Now.FirstDayOfWeek();

            model.ToDay = DateTime.Now.Day.ToString();
            model.ToMonth = DateTime.Now.Month.ToString();
            model.ToYear = DateTime.Now.Year.ToString();
            model.FromDay = firstDayOfWeek.Day.ToString();
            model.FromMonth = firstDayOfWeek.Month.ToString();
            model.FromYear = firstDayOfWeek.Year.ToString();
        }

        return Result.Success(model);
    }

    private async Task<Result<IActionResult>> HandleSuccess(
        InternalUser user, 
        FellingLicenceApplicationsReportQueryResultModel reportData,
        FellingLicenceApplicationsReportQuery query,
        CancellationToken cancellationToken)
    {
        IActionResult actionResult;

        if (reportData.HasData)
        {
            _logger.LogDebug("Found [{FlaDataCount}] records for main FLA data set.", reportData.FellingLicenceApplicationReportEntries.Count);

            var archiveInfo = ReportDataZipArchiveHelper.GetAllReportsForArchive(reportData, _recyclableMemoryStreamManager);

            var fileNameTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc().ToString("yyyy-MMM-dd_HHmmss");
            var zipFileName = $"{ZipFileNamePrefix}-{fileNameTimeStamp}.zip";

            _logger.LogDebug("Found [{FlaDataCount}] records for main FLA data set. Writing ZIP archive named [{zipName}], containing [{zipEntries}] files.",
                reportData.FellingLicenceApplicationReportEntries.Count, zipFileName, archiveInfo.Count);

            actionResult = await ReportDataZipArchiveHelper.CreateZipArchiveForDataDownloadAsync(archiveInfo, _recyclableMemoryStreamManager, zipFileName);

            await AddAudit(AuditEvents.ReportExecution, Guid.Empty, user.UserAccountId!.Value, new
            {
                ZipArchiveFileName = zipFileName, 
                MainFlaDataSetRowCount = reportData.FellingLicenceApplicationReportEntries.Count,
                QueryModel = query
            }, //poss overkill/destructive
                cancellationToken);

            _logger.LogDebug("ZIP archive named [{zipFileName}] created.", zipFileName);
        }
        else
        {
            _logger.LogDebug("No data was found for report executed, so no ZIP will be created, nor returned.");
            
            actionResult = new EmptyResult();

            await AddAudit(
                AuditEvents.ReportExecutionNoDataFound, 
                Guid.Empty, 
                user.UserAccountId!.Value, 
                new { QueryModel = query }, 
                cancellationToken);
        }

        return Result.Success(actionResult);
    }

    private async Task<Result<IActionResult>> HandleFailure(
        InternalUser user, 
        string error, 
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Query for report resulted in failure. Failure error: [{queryResultError}]", error);
        await AddAudit(AuditEvents.ReportExecutionFailure, Guid.Empty, user.UserAccountId!.Value, new { Error = error }, cancellationToken);
        return Result.Failure<IActionResult>("Unable to create report.");
    }

    private async Task AddAudit(string auditEventName, Guid entityGuid, Guid userGuid, object? auditData = null, CancellationToken cancellationToken = default)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            auditEventName,
            entityGuid,
            userGuid,
            _requestContext,
            auditData), cancellationToken);
    }
}
