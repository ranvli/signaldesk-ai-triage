using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SignalDesk.Infrastructure.Data;

public class SignalDeskDbContextFactory : IDesignTimeDbContextFactory<SignalDeskDbContext>
{
    public SignalDeskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SignalDeskDbContext>()
            .UseSqlite("Data Source=signaldesk.db")
            .Options;

        return new SignalDeskDbContext(options);
    }
}
