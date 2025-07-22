using CSharpFunctionalExtensions;

namespace Forestry.Flo.Services.Common;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task <UnitResult<UserDbErrorReason>> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}