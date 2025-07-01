using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Models;
namespace MyProject.Infrastructure.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) 
        { 

        }
        public DbSet<Product> Product { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /**
             * HasQueryFilter全局过滤条件
             * 可在软删除或者根据当前租户ID过滤数据，确保每个租户只能访问自己的数据。
             * **/
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            //base.OnModelCreating(modelBuilder);
        }
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                }
                else
                {
                    entity.UpdatedDate = DateTime.UtcNow;
                }
            }
        }
    }
}
