using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Services.Applicants.Tests.Repositories;

public class LegacyDocumentsRepositoryTests
{
    private readonly ApplicantsContext _applicantsContext;
    private readonly LegacyDocumentsRepository _sut;

    public LegacyDocumentsRepositoryTests()
    {
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();
        _sut = new LegacyDocumentsRepository(_applicantsContext);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldRetrieveAllLegacyDocuments(TestApplicantsDatabaseFactory.TestLegacyDocument[] documents)
    {
        await ((TestApplicantsDatabaseFactory.TestApplicantsContext)_applicantsContext).AddLegacyDocuments(
            documents, CancellationToken.None);

        var result = await _sut.GetAllLegacyDocumentsAsync(CancellationToken.None);

        AssertMatchingLists(documents, result);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldRetrieveLegacyDocumentsForWoodlandOwnerId(TestApplicantsDatabaseFactory.TestLegacyDocument[] documents)
    {
        await ((TestApplicantsDatabaseFactory.TestApplicantsContext)_applicantsContext).AddLegacyDocuments(
            documents, CancellationToken.None);

        var woodlandOwnerIds = new List<Guid>
        {
            documents.First().WoodlandOwnerId,
            documents.Last().WoodlandOwnerId
        };

        var result = await _sut.GetAllForWoodlandOwnerIdsAsync(woodlandOwnerIds, CancellationToken.None);

        AssertMatchingLists(new[] {documents.First(), documents.Last()}, result);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldRetrieveSpecificLegacyDocument(TestApplicantsDatabaseFactory.TestLegacyDocument[] documents)
    {
        await ((TestApplicantsDatabaseFactory.TestApplicantsContext)_applicantsContext).AddLegacyDocuments(
            documents, CancellationToken.None);

        var result = await _sut.GetAsync(documents.First().Id, CancellationToken.None);

        AssertMatchingLists(new[] {documents.First()}, new [] {result.Value});
    }

    private void AssertMatchingLists(
        TestApplicantsDatabaseFactory.TestLegacyDocument[] expected,
        IEnumerable<LegacyDocument> actual)
    {
        Assert.Equal(expected.Length, actual.Count());

        foreach (var document in expected)
        {
            Assert.Contains(actual, x =>
                x.Id == document.Id
                && x.WoodlandOwnerId == document.WoodlandOwnerId
                && x.DocumentType == document.DocumentType
                && x.FileName == document.FileName
                && x.FileSize == document.FileSize
                && x.FileType == document.FileType
                && x.MimeType == document.MimeType
                && x.Location == document.Location);
        }
    }

}