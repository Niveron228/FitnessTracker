using CaloriesTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CaloriesTracker.DB
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Foods> Foods { get; set; }

        public DbSet<Users> Users { get; set; }


    }
}
