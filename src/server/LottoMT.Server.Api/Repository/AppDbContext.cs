using Microsoft.EntityFrameworkCore;

namespace LottoMT.Server.Api.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
