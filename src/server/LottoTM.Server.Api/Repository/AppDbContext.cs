using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
