using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using LinqKit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

public class FellingLicenceApplicationsReportQuery
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public ReportDateRangeType? DateRangeTypeForReport { get; set; }
    public List<FellingOperationType> ConfirmedFellingOperationTypes { get; } = new();
    public List<FellingOperationType> ProposedFellingOperationTypes { get; } = new();
    public FellingLicenceStatus? CurrentStatus { get; set; }
    public List<string> ConfirmedFellingSpecies { get; set; } = new();
    public List<Guid> SelectedAdminHubIds { get; set; } = new();
    public List<Guid> AssociatedAdminHubUserIds { get; set; } = new();
    public Guid? SelectedAdminOfficerId { get; set; }
    public Guid? SelectedWoodlandOfficerId { get; set; }

    internal IQueryable<FellingLicenceApplication> Apply(
        IQueryable<FellingLicenceApplication> baseQuery)
    {
        var result = baseQuery.AsExpandable();

        if (ConfirmedFellingSpecies.Any())
        {
            result = result.Where(x =>
                x.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.AsQueryable()
                    .Any(ReportQueryExtensions.CompartmentsHaveConfirmedFellingSpecies(ConfirmedFellingSpecies.ToArray())));
        }
        
        if (ConfirmedFellingOperationTypes.Any())
        {
            result = result.Where(x => x.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.AsQueryable()
                .Any(c => c.ConfirmedFellingDetails != null && c.ConfirmedFellingDetails
                            .Any(detail => ConfirmedFellingOperationTypes.Contains(detail.OperationType))));
        }

        if (CurrentStatus.HasValue)
        {
            //note Linq MaxBy doesn't work in this context..
            result = result.Where(x => x.StatusHistories.OrderByDescending(c=>c.Created)
                .FirstOrDefault(c => c.Created >= DateFrom && c.Created <= DateTo)!.Status == CurrentStatus);
        }

        if (SelectedAdminHubIds.Any() && AssociatedAdminHubUserIds.Any())
        {
            result = result.Where(ReportQueryExtensions.HasAssignedUsers(AssociatedAdminHubUserIds));
        }

        if (SelectedAdminOfficerId.HasValue)
        {
            result = result.Where(x => x.AssigneeHistories.AsQueryable()
                .Any(h => h.TimestampUnassigned.HasValue == false
                          && h.AssignedUserId == SelectedAdminOfficerId
                          && h.Role == AssignedUserRole.AdminOfficer));
        }

        if (SelectedWoodlandOfficerId.HasValue)
        {
            result = result.Where(x => x.AssigneeHistories.AsQueryable()
                .Any(h => h.TimestampUnassigned.HasValue == false
                          && h.AssignedUserId == SelectedWoodlandOfficerId
                          && h.Role == AssignedUserRole.WoodlandOfficer));
        }
        
        result = DateRangeTypeForReport switch
        {
            ReportDateRangeType.Approved => result.Where(s =>
                s.StatusHistories.Any(x => x.Status == FellingLicenceStatus.Approved && x.Created >= DateFrom && x.Created <= DateTo)),
            ReportDateRangeType.Submitted => result.Where(s =>
                s.StatusHistories.Any(x => x.Status == FellingLicenceStatus.Submitted && x.Created >= DateFrom && x.Created <= DateTo)),
            ReportDateRangeType.FinalAction => result.Where(x =>
                x.FinalActionDate >= DateFrom && x.FinalActionDate <= DateTo),
            ReportDateRangeType.OnPublicRegister => result.Where(x =>
                x.PublicRegister!.ConsultationPublicRegisterPublicationTimestamp >= DateFrom && x.PublicRegister!.ConsultationPublicRegisterPublicationTimestamp <= DateTo),
            ReportDateRangeType.OffPublicRegister => result.Where(x =>
                x.PublicRegister!.ConsultationPublicRegisterRemovedTimestamp >= DateFrom && x.PublicRegister!.ConsultationPublicRegisterRemovedTimestamp <= DateTo),
            ReportDateRangeType.PublicRegisterExpiry => result.Where(x =>
                x.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp >= DateFrom && x.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp <= DateTo),
            ReportDateRangeType.CitizensCharter => result.Where(x =>
                x.CitizensCharterDate >= DateFrom && x.CitizensCharterDate <= DateTo),
            ReportDateRangeType.CompletedFelling => result.Where(x =>
                x.ActualFellingEnd >= DateFrom && x.ActualFellingEnd <= DateTo),
            ReportDateRangeType.ReferredToLocalAuthority => result.Where(s =>
                s.StatusHistories.Any(x => x.Status == FellingLicenceStatus.ReferredToLocalAuthority && x.Created >= DateFrom && x.Created <= DateTo)),

            _ => result
        };

        return result;
    }

    public override string ToString()
    {
        return $"{nameof(DateFrom)}: [{DateFrom}], " +
               $"{nameof(DateTo)}: [{DateTo}], " +
               $"{nameof(DateRangeTypeForReport)}: [{DateRangeTypeForReport}], " +
               $"{nameof(CurrentStatus)}: [{CurrentStatus}]." +
               $"{nameof(SelectedAdminOfficerId)}: [{SelectedAdminOfficerId }]." +
               $"{nameof(SelectedWoodlandOfficerId )}: [{SelectedWoodlandOfficerId }]." +
               $"{nameof(SelectedAdminHubIds)}: [{string.Join(',', SelectedAdminHubIds) }]." +
               $"Count Of filtered {nameof(ConfirmedFellingSpecies)}: [{ConfirmedFellingSpecies.Count}], "+
               $"Count Of filtered {nameof(ConfirmedFellingOperationTypes)}: [{ConfirmedFellingOperationTypes.Count}], ";
    }
}
