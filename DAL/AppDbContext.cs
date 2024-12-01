using ConsoleApp5.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp5.DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Shop> Shop { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<StockItem> StockItem { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
