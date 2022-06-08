using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabricdot.Infrastructure.Uow.Abstractions;

public interface ITransactionFacade : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}