using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Repositories;

public class AgencyRepository : IAgencyRepository
{
    private readonly ApplicantsContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public AgencyRepository(ApplicantsContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public async Task<Result<Agency, UserDbErrorReason>> AddAgencyAsync(
        Agency entity, 
        CancellationToken cancellationToken)
    {
        var result = (await _context.Agencies.AddAsync(entity, cancellationToken)).Entity;
        return await UnitOfWork.SaveEntitiesAsync(cancellationToken).Map(() => result);
    }

    ///<inheritdoc />
    public async Task<Result<Agency, UserDbErrorReason>> GetAsync(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var agency = await _context
            .Agencies
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return agency == null 
            ? Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<Agency, UserDbErrorReason>(agency);
    }

    ///<inheritdoc />
    public async Task<Result> DeleteAgencyAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var agency = await _context
            .Agencies
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        if (agency is null)
        {
            return Result.Failure("Unable to retrieve agency with given id");
        }

        var authorities = await ListAuthoritiesByAgencyAsync(
            agency.Id, 
            null, 
            cancellationToken);

        if (authorities.IsFailure)
        {
            return Result.Failure("Unable to retrieve agent authorities");
        }

        if (authorities.Value.Any())
        {
            return Result.Failure("Unable to remove agency with agent authorities");
        }

        _context.Agencies.Remove(agency);

        var saveResult = await UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result.Success()
            : Result.Failure(saveResult.ToString());
    }

    ///<inheritdoc />
    public async Task<Result<AgentAuthority, UserDbErrorReason>> GetAgentAuthorityAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.AgentAuthorities
            .Include(a => a.WoodlandOwner)
            .Include(a => a.Agency)
            .Include(a => a.CreatedByUser)
            .Include(a => a.AgentAuthorityForms)
                .ThenInclude(a => a.AafDocuments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity == null
            ? Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<AgentAuthority, UserDbErrorReason>(entity);
    }

    ///<inheritdoc />
    public async Task<Maybe<AgentAuthorityStatus>> FindAgentAuthorityStatusAsync(Guid agencyId, Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var entity = await _context.AgentAuthorities.FirstOrDefaultAsync(
            x => x.Agency.Id == agencyId && x.WoodlandOwner.Id == woodlandOwnerId,
            cancellationToken);

        return entity != null
            ? Maybe.From(entity.Status)
            : Maybe<AgentAuthorityStatus>.None;
    }

    ///<inheritdoc />
    public async Task<Maybe<AgentAuthority>> FindAgentAuthorityAsync(Guid agencyId, Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var entity = await _context.AgentAuthorities
            .Include(x => x.AgentAuthorityForms)
            .FirstOrDefaultAsync(x => x.Agency.Id == agencyId && x.WoodlandOwner.Id == woodlandOwnerId, cancellationToken);

        return entity.AsMaybe();
    }

    ///<inheritdoc />
    public async Task<Result<IEnumerable<AgentAuthority>, UserDbErrorReason>> ListAuthoritiesByAgencyAsync(
        Guid agencyId, 
        AgentAuthorityStatus[]? filter, 
        CancellationToken cancellationToken)
    {
        var entities = await _context.AgentAuthorities
            .Include(a => a.WoodlandOwner)
            .Include(a => a.Agency)
            .Include(a => a.CreatedByUser)
            .Include(a => a.AgentAuthorityForms)
                .ThenInclude(a => a.AafDocuments)
            .Where(a => a.Agency.Id == agencyId)
            .ToListAsync(cancellationToken);

        if (filter != null)
        {
            entities = entities.Where(x => filter.Contains(x.Status)).ToList();
        }
            
        return Result.Success<IEnumerable<AgentAuthority>, UserDbErrorReason>(entities);
    }
    
    ///<inheritdoc />
    public async Task<Maybe<AgentAuthority>> GetActiveAuthorityByWoodlandOwnerIdAsync(
        Guid woodlandOwnerId, 
        CancellationToken cancellationToken)
    {
        var aaf = await _context
            .AgentAuthorities
            .Include(x => x.Agency)
            .Where(x => x.WoodlandOwner.Id == woodlandOwnerId && x.Status != AgentAuthorityStatus.Deactivated)
            .FirstOrDefaultAsync(cancellationToken);

        return aaf == null
            ? Maybe<AgentAuthority>.None
            : Maybe<AgentAuthority>.From(aaf);
    }

    ///<inheritdoc />
    public async Task<Result<AgentAuthority, UserDbErrorReason>> AddAgentAuthorityAsync(
        AgentAuthority entity, 
        CancellationToken cancellationToken)
    {
        var result = (await _context.AgentAuthorities.AddAsync(entity, cancellationToken)).Entity;

        return await UnitOfWork.SaveEntitiesAsync(cancellationToken).Map(() => result);
    }

    public async Task<UnitResult<UserDbErrorReason>> DeleteAgentAuthorityAsync(Guid agentAuthorityId, CancellationToken cancellationToken)
    {
        var aaf = await _context
            .AgentAuthorities
            .Where(x => x.Id == agentAuthorityId)
            .Include(x => x.AgentAuthorityForms)
                .ThenInclude(a => a.AafDocuments)
            .SingleOrDefaultAsync(cancellationToken);

        if (aaf == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        _context.AgentAuthorities.Remove(aaf);
        return await UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<Maybe<Agency>> FindAgencyForWoodlandOwnerAsync(Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var aaf = await _context
            .AgentAuthorities
            .Include(x => x.Agency)
            .Where(x => x.WoodlandOwner.Id == woodlandOwnerId && x.Status != AgentAuthorityStatus.Deactivated)
            .FirstOrDefaultAsync(cancellationToken);

        return aaf == null
            ? Maybe<Agency>.None
            : Maybe<Agency>.From(aaf.Agency);
    }

    ///<inheritdoc />
    public async Task<Maybe<Agency>> FindFcAgency(CancellationToken cancellationToken)
    {
        var result = await _context
            .Agencies
            .SingleOrDefaultAsync(x => x.IsFcAgency, cancellationToken);

        return result == null
            ? Maybe<Agency>.None
            : Maybe<Agency>.From(result);
    }

    ///<inheritdoc />
    public async Task<List<AgentAuthority>> GetActiveAgentAuthoritiesAsync(CancellationToken cancellationToken)
    {
        return await _context.AgentAuthorities
            .Include(aa => aa.Agency)
            .Include(aa => aa.WoodlandOwner)
            .Where(x => x.Status != AgentAuthorityStatus.Deactivated)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<List<Agency>> GetAgenciesForActiveAccountsAsync(CancellationToken cancellationToken)
    {
        return await _context.UserAccounts
            .Include(x => x.Agency)
            .Where(x => x.Status != UserAccountStatus.Deactivated && x.Agency != null)
            .Select(x => x.Agency!)
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<List<Agency>> GetAgenciesWithIdNotIn(List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _context.Agencies
            .Where(x => !ids.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}