using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
