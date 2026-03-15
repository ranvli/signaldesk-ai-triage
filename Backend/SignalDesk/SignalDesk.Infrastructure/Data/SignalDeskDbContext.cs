using Microsoft.EntityFrameworkCore;
using SignalDesk.Domain.Entities;

namespace SignalDesk.Infrastructure.Data;

public class SignalDeskDbContext(DbContextOptions<SignalDeskDbContext> options) : DbContext(options)
{
    public DbSet<FeedbackItem> FeedbackItems => Set<FeedbackItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SignalDeskDbContext).Assembly);
    }
}
