using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// EF caches one model per context type by default. The branch scope is part
/// of the query filter, so it must also be part of the model cache key.
/// </summary>
public sealed class BranchModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), BranchScope.CurrentBranchId, designTime);
}

public static class LisDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseBranchScope(this DbContextOptionsBuilder builder) =>
        builder.ReplaceService<IModelCacheKeyFactory, BranchModelCacheKeyFactory>();
}
