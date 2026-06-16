using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFCHub.Infrastructure.Persistence;

public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is AppDbContext appDbContext)
        {
            return (context.GetType(), appDbContext.SchemaName, designTime);
        }

        return (context.GetType(), designTime);
    }
}
