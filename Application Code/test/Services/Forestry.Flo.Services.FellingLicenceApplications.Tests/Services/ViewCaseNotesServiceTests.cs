using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ViewCaseNotesServiceTests
{
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;


    public ViewCaseNotesServiceTests()
    {
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
    }

    [Fact]
    public async Task WhenCaseNotesExist_CheckBasicFetchScenario()
    {
        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        Guid fellingLicenceApplicationId = Guid.NewGuid();

        _fellingLicenceApplicationRepository.Setup(x => x.GetCaseNotesAsync(fellingLicenceApplicationId, true, CancellationToken.None)).ReturnsAsync(new List<CaseNote>
        {
            new()
            {
                CreatedByUserId = userId1,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            },
            new()
            {
                CreatedByUserId = userId2,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            }
        });

        _userAccountRepository.Setup(x => x.GetUsersWithIdsInAsync(new List<Guid> { userId1, userId2 }, CancellationToken.None)).ReturnsAsync(new List<UserAccount>
        {
            new UserAccountProxy(userId1),
            new UserAccountProxy(userId2)
        });

        //arrange
        var sut = new ViewCaseNotesService(_fellingLicenceApplicationRepository.Object, _userAccountRepository.Object);

        //act
        var result = await sut.GetCaseNotesAsync(fellingLicenceApplicationId, true, CancellationToken.None);

        //assert

        Assert.True(result.Count(x => x.CreatedByUserId == userId1) == 1);
        Assert.True(result.Count(x => x.CreatedByUserId == userId2) == 1);
        Assert.True(result.Count(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId) == 2);
    }

    [Theory, AutoMoqData]
    public async Task WhenCaseNotesExist_GetSpecificCaseNotes(CaseNoteType[] types)
    {
        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        Guid fellingLicenceApplicationId = Guid.NewGuid();

        _fellingLicenceApplicationRepository.Setup(x => x.GetCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), CancellationToken.None)).ReturnsAsync(new List<CaseNote>
        {
            new()
            {
                CreatedByUserId = userId1,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            },
            new()
            {
                CreatedByUserId = userId2,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            }
        });

        _userAccountRepository.Setup(x => x.GetUsersWithIdsInAsync(new List<Guid> { userId1, userId2 }, CancellationToken.None)).ReturnsAsync(new List<UserAccount>
        {
            new UserAccountProxy(userId1),
            new UserAccountProxy(userId2)
        });

        //arrange
        var sut = new ViewCaseNotesService(_fellingLicenceApplicationRepository.Object, _userAccountRepository.Object);

        //act
        var result = await sut.GetSpecificCaseNotesAsync(fellingLicenceApplicationId, types, CancellationToken.None);

        //assert

        Assert.True(result.Count(x => x.CreatedByUserId == userId1) == 1);
        Assert.True(result.Count(x => x.CreatedByUserId == userId2) == 1);
        Assert.True(result.Count(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId) == 2);
    }

    [Fact] 
    public async Task WhenVisibleToApplicantOnlyIsTrue_OnlyApplicantVisibleCommentsAreReturned()
    {
        //arrange

        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        Guid fellingLicenceApplicationId = Guid.NewGuid();

        // Set up repository responses for true / false scenarios on visibleToApplicantOnly arg

        _fellingLicenceApplicationRepository.Setup(x => x.GetCaseNotesAsync(fellingLicenceApplicationId, true, CancellationToken.None)).ReturnsAsync(new List<CaseNote>
        {
            new()
            {
                CreatedByUserId = userId1,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            }
        });

        _fellingLicenceApplicationRepository.Setup(x => x.GetCaseNotesAsync(fellingLicenceApplicationId, false, CancellationToken.None)).ReturnsAsync(new List<CaseNote>
        {
            new()
            {
                CreatedByUserId = userId1,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = true,
                Type = CaseNoteType.ReturnToApplicantComment,
                VisibleToConsultee = true
            },
            new()
            {
                CreatedByUserId = userId2,
                CreatedTimestamp = DateTime.Now,
                FellingLicenceApplicationId = fellingLicenceApplicationId,
                Id = Guid.NewGuid(),
                Text = "Test1",
                VisibleToApplicant = false,
                Type = CaseNoteType.SiteVisitComment,
                VisibleToConsultee = true
            }
        });

        _userAccountRepository.Setup(x => x.GetUsersWithIdsInAsync(new List<Guid> { userId1 }, CancellationToken.None)).ReturnsAsync(new List<UserAccount>
        {
            new UserAccountProxy(userId1)
        });

        _userAccountRepository.Setup(x => x.GetUsersWithIdsInAsync(new List<Guid> { userId1, userId2 }, CancellationToken.None)).ReturnsAsync(new List<UserAccount>
        {
            new UserAccountProxy(userId1),
            new UserAccountProxy(userId2)
        });

        var sut = new ViewCaseNotesService(_fellingLicenceApplicationRepository.Object, _userAccountRepository.Object);

        //act
        var result1 = await sut.GetCaseNotesAsync(fellingLicenceApplicationId, true, CancellationToken.None);
        var result2 = await sut.GetCaseNotesAsync(fellingLicenceApplicationId, false, CancellationToken.None);

        //assert

        Assert.True(result1.Count(x => x.CreatedByUserId == userId1) == 1);
        Assert.True(result1.Count(x => x.CreatedByUserId == userId2) == 0);
        Assert.True(result1.Count(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId) == 1);

        Assert.True(result2.Count(x => x.CreatedByUserId == userId1) == 1);
        Assert.True(result2.Count(x => x.CreatedByUserId == userId2) == 1);
        Assert.True(result2.Count(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId) == 2);
    }

    /// <summary>
    /// Proxy class facilitates setting of protected Id
    /// </summary>
    private class UserAccountProxy : UserAccount
    {
        public UserAccountProxy(Guid id)
        {
            this.Id = id;
        }
    }
}