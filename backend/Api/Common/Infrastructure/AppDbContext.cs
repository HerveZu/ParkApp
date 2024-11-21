using Api.Common.Options;
using Api.Common.Reflexion;
using Domain;
using EntityFramework.Extensions.AddQueryFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Common.Infrastructure;

internal sealed class AppDbContext(
    IOptions<PostgresOptions> postgresOptions,
    IHttpContextAccessor httpContextAccessor
) : DbContext
{
    private string? CurrentUserIdentity => httpContextAccessor.HttpContext?.User.Identity?.Name;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(postgresOptions.Value.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configurationType = typeof(IEntityConfiguration<>);
        var assembly = configurationType.Assembly;

        modelBuilder.ApplyConfigurationsFromAssembly(
            assembly,
            type => type.IsAssignableToGenericType(configurationType)
        );

        modelBuilder.AddQueryFilterOnAllEntities<IUserResource>(
            entity => entity.UserIdentity == CurrentUserIdentity);
    }
}
