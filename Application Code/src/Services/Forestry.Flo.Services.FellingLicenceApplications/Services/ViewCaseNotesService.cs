using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ViewCaseNotesService : IViewCaseNotesService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;

    public ViewCaseNotesService(
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
        IUserAccountRepository userAccountRepository
    )
    {
        _fellingLicenceApplicationRepository = fellingLicenceApplicationRepository;
        _userAccountRepository = userAccountRepository;
    }

    public async Task<IList<CaseNoteModel>> GetCaseNotesAsync(Guid applicationId, bool visibleToApplicantOnly, CancellationToken cancellationToken)
    {
        var caseNotes = await _fellingLicenceApplicationRepository.GetCaseNotesAsync(applicationId, visibleToApplicantOnly, cancellationToken);

        return await GetCaseNoteModelsAsync(caseNotes, cancellationToken);
    }

    public async Task<IList<CaseNoteModel>> GetSpecificCaseNotesAsync(Guid applicationId, CaseNoteType[] caseNoteTypes, CancellationToken cancellationToken)
    {
        var caseNotes = await _fellingLicenceApplicationRepository.GetCaseNotesAsync(applicationId, caseNoteTypes, cancellationToken);

        return await GetCaseNoteModelsAsync(caseNotes, cancellationToken);
    }

    private async Task<IList<CaseNoteModel>> GetCaseNoteModelsAsync(IList<CaseNote> caseNotes, CancellationToken cancellationToken)
    {
        var users = await _userAccountRepository.GetUsersWithIdsInAsync(caseNotes.Select(x => x.CreatedByUserId).ToList(), cancellationToken);

        var caseNoteModels = new List<CaseNoteModel>();

        foreach (var caseNote in caseNotes)
        {
            caseNoteModels.Add(new CaseNoteModel
            {
                Id = caseNote.Id,
                FellingLicenceApplicationId = caseNote.FellingLicenceApplicationId,
                Type = caseNote.Type,
                Text = caseNote.Text,
                VisibleToApplicant = caseNote.VisibleToApplicant,
                VisibleToConsultee = caseNote.VisibleToConsultee,
                CreatedByUserId = caseNote.CreatedByUserId,
                CreatedByUserName = users.Value.SingleOrDefault(x => x.Id == caseNote.CreatedByUserId)?.FullNameNoTitle() ?? string.Empty,
                CreatedByUserAccountType = users.Value.SingleOrDefault(x => x.Id == caseNote.CreatedByUserId)?.AccountType ?? AccountTypeInternal.FcStaffMember,
                CreatedTimestamp = caseNote.CreatedTimestamp
            });
        }

        return caseNoteModels;
    }
}
