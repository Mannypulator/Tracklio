namespace Tracklio.Shared.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


public class RepositoryContextFactory : IDesignTimeDbContextFactory<RepositoryContext>
{
    public RepositoryContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RepositoryContext>();

        // Load from environment or fallback for design-time
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        optionsBuilder.UseNpgsql(connectionString);

        return new RepositoryContext(optionsBuilder.Options);
    }
}
