using Microsoft.EntityFrameworkCore;
using SharedContracts;

namespace JobApi;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
}
