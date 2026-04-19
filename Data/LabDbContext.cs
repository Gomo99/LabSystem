using Microsoft.EntityFrameworkCore;

namespace LaboratoryTestRequestManagementSystem.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext(DbContextOptions options) : base(options)
        {
        }








        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}