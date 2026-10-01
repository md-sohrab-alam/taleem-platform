using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Taleem.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaleemDbContext>
{
    public TaleemDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaleemDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=taleem;Username=taleem;Password=taleem",
                o => o.UseVector())
            .Options;
        return new TaleemDbContext(options);
    }
}
