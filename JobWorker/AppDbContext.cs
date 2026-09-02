using Microsoft.EntityFrameworkCore;
using SharedContracts;

namespace JobWorker;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
}
