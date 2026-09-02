using Microsoft.EntityFrameworkCore;

namespace SharedContracts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
}
