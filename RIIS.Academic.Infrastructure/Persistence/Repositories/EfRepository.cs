using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Application.Abstractions.Persistence;

namespace RIIS.Academic.Infrastructure.Persistence.Repositories;

public class EfRepository<TEntity>(RiisAcademicDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    private readonly SemaphoreSlim operationGate = new(1, 1);

    public async Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await context.Set<TEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await context.Set<TEntity>().FindAsync([id], cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public void Delete(TEntity entity)
    {
        operationGate.Wait();
        try
        {
            context.Set<TEntity>().Remove(entity);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var entity = await context.Set<TEntity>().FindAsync([id], cancellationToken);
            if (entity is not null)
            {
                context.Set<TEntity>().Remove(entity);
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }
}
