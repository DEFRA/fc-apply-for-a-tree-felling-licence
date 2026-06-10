using Ardalis.GuardClauses;
using Forestry.Flo.Services.Applicants.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Repositories;

/// <summary>
/// Implementation of the <see cref="IApplicantRepository"/> interface for retrieving Applicant data from the underlying database.
/// </summary>
public class ApplicantRepository(ApplicantsContext applicantsContext) : IApplicantRepository
{
    private readonly ApplicantsContext _applicantsContext = Guard.Against.Null(applicantsContext);

    /// <inheritdoc/>
    public async Task<int> GetApplicantsCountAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var result = await ApplySearchTerm(searchTerm).CountAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public IEnumerable<Applicant> GetApplicants(
        string? searchTerm, 
        string sortColumn, 
        bool sortAscending, 
        int pageNumber, 
        int pageSize)
    {
        var searched = ApplySearchTerm(searchTerm);

        var sorted = sortColumn switch
        {
            nameof(Applicant.Name) => sortAscending ? searched.OrderBy(a => a.Name) : searched.OrderByDescending(a => a.Name),
            nameof(Applicant.Email) => sortAscending ? searched.OrderBy(a => a.Email) : searched.OrderByDescending(a => a.Email),
            nameof(Applicant.ManagedBy) => sortAscending ? searched.OrderBy(a => a.ManagedBy) : searched.OrderByDescending(a => a.ManagedBy),
            nameof(Applicant.Type) => sortAscending ? searched.OrderBy(a => a.Type) : searched.OrderByDescending(a => a.Type),
            _ => searched
        };

        var paged = sorted.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return paged.AsEnumerable();
    }

    private IQueryable<Applicant> ApplySearchTerm(string? searchTerm)
    {
        var query = _applicantsContext.Applicants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a => 
                a.Name.ToLower().Contains(searchTerm.ToLower()) 
                || a.Email.ToLower().Contains(searchTerm.ToLower())
                || a.ManagedBy.ToLower().Contains(searchTerm.ToLower()));
        }
        return query;
    }
}