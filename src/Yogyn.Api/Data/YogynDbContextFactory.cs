using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Yogyn.Api.Data;

public class YogynDbContextFactory : IDesignTimeDbContextFactory<YogynDbContext>
{
    public YogynDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<YogynDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new YogynDbContext(optionsBuilder.Options);
    }
}