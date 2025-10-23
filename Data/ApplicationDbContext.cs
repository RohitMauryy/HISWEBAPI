using HISWEBAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HISWEBAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LoginModel> LoginDetails { get; set; }
        public DbSet<BranchModel> BranchDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LoginModel>().HasNoKey(); 
            modelBuilder.Entity<BranchModel>().HasNoKey();
            base.OnModelCreating(modelBuilder);
        }
    }
}
