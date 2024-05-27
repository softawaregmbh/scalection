using Microsoft.EntityFrameworkCore;

namespace Scalection.ApiService
{
    public class ScalectionContext : DbContext
    {
        public ScalectionContext(DbContextOptions<ScalectionContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
