using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.Reports;

public class ReportRequestViewModel
{
    [Required(ErrorMessage = "Day from must be provided")]
    [DisplayName("Day")]
    public string? FromDay { get; set; }

    [Required(ErrorMessage = "Month from must be provided")]
    [DisplayName("Month")]
    public string? FromMonth { get; set; }

    [Required(ErrorMessage = "Year from must be provided")]
    [DisplayName("Year")]
    public string? FromYear { get; set; }

    [Required(ErrorMessage = "Day to must be provided")]
    [DisplayName("Day")]
    public string? ToDay { get; set; }

    [Required(ErrorMessage = "Month to must be provided")]
    [DisplayName("Month")]
    public string? ToMonth { get; set; }

    [Required(ErrorMessage = "Year to must be provided")]
    [DisplayName("Year")]
    public string? ToYear { get; set; }

    public List<AdminHubModel> AdminHubs { get; set; } = new();
    public List<Guid> SelectedAdminHubIds { get; set; } = new();
    public List<ConfirmedFcUserModelForReporting> ConfirmedFcUsers { get; set; } = new();
    public Guid? SelectedWoodlandOfficerId { get; set; }
    public Guid? SelectedAdminOfficerId { get; set; }
    public DateRangeTypeForReporting DateRangeType { get; set; }
    public FellingLicenceApplicationStatusesForReporting? CurrentStatus { get; set; }

    [BindProperty]
    [ModelBinder(Name = "confirmed-felling-tree-species-filters")]
    public List<SpeciesModelForReporting> SelectedConfirmedFellingSpecies { get; set; } = new();

    [BindProperty]
    [ModelBinder(Name = "confirmed-felling-type-filters")]
    public List<FellingOperationTypeForReporting> SelectedConfirmedFellingOperationTypes { get; set; } = new();

    [BindProperty]
    [ModelBinder(Name = "proposed-felling-type-filters")]
    public List<FellingOperationTypeForReporting> SelectedProposedFellingOperationTypes { get; set; } = new();

    public DateTime? FromDateTime => TryCreateDateFromParts(FromYear, FromMonth, FromDay);

    public DateTime? ToDateTime => TryCreateDateFromParts(ToYear, ToMonth, ToDay);

    private static DateTime? TryCreateDateFromParts(string? year, string? month, string? day)
    {
        if (string.IsNullOrEmpty(year) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(day))
        {
            return null;
        }

        if (DateTime.TryParseExact($"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Converts the view model instance to the model required by the <see cref="Flo.Services.FellingLicenceApplications.Services.IReportQueryService"/> reporting  service.
    /// </summary>
    /// <returns></returns>
    public Result<FellingLicenceApplicationsReportQuery> ToQuery()
    {
        if (!FromDateTime.HasValue || !ToDateTime.HasValue)
        {
            return Result.Failure<FellingLicenceApplicationsReportQuery>($"Query for report specified invalid date range: {FromDateTime} {ToDateTime}.");
        }

        var query = new FellingLicenceApplicationsReportQuery
        {
            DateRangeTypeForReport = DateRangeType.ToReportDateRangeType(),
            CurrentStatus = CurrentStatus?.ToEntityFellingLicenceStatus()

        };
        
        MapFellingTypeOperations(query.ConfirmedFellingOperationTypes, SelectedConfirmedFellingOperationTypes);
        MapFellingTypeOperations(query.ProposedFellingOperationTypes, SelectedProposedFellingOperationTypes);

        if (SelectedConfirmedFellingSpecies.Any())
        {
            query.ConfirmedFellingSpecies = SelectedConfirmedFellingSpecies.Select(x => x.Key).ToList();
        }

        if (FromDateTime.Value <= ToDateTime.Value)
        {
            query.DateFrom = DateTime.SpecifyKind(FromDateTime.Value.Date, DateTimeKind.Utc);
            query.DateTo = DateTime.SpecifyKind(ToDateTime.Value.Date.AddDays(1), DateTimeKind.Utc);
        }
        else
        {
            query.DateFrom = DateTime.SpecifyKind(ToDateTime.Value.Date, DateTimeKind.Utc);
            query.DateTo = DateTime.SpecifyKind(FromDateTime.Value.Date.AddDays(1), DateTimeKind.Utc);
        }

        if (SelectedAdminHubIds.Any())
        {
            query.SelectedAdminHubIds = SelectedAdminHubIds;
        }

        query.SelectedAdminOfficerId = SelectedAdminOfficerId;
        query.SelectedWoodlandOfficerId = SelectedWoodlandOfficerId;

        return Result.Success(query);
    }

    private static void MapFellingTypeOperations(
        List<FellingOperationType> queryModelProperty,
        List<FellingOperationTypeForReporting> selected)
    {
        foreach (var fellingOperationTypeForReporting in selected)
        {
            queryModelProperty.Add(fellingOperationTypeForReporting.ToFellingOperationType());
        }
    }
}
