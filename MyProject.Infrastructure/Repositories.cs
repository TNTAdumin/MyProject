using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyProject.Domain.Common;
using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;
using MyProject.Infrastructure.Data;
using NPOI.OpenXmlFormats.Dml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Infrastructure
{
    /**
     *仓储实现 
     **/
    public class Repositories<T> : IProductRepository<T> where T : BaseEntity
    {
        private readonly MyDbContext _context;
        private readonly DbSet<T> _dbSet;
        public Repositories(MyDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task AddAsync(T product, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
                _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public async Task<T> GetProductAsync(int id, string productName, CancellationToken cancellationToken = default) => await _dbSet.FindAsync(id);

        public async Task<IPagedList<T>> GetPagedListAsync(int pageIndex, int pageSize)
        {
            var query = _dbSet.AsQueryable();
            return await PagedList<T>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<IQueryable<T>> GetQueryAsync<TResult>(Expression<Func<T, bool>> predicate)
        {
            return await Task.FromResult(_dbSet.Where(predicate));
        }

        public async Task<IQueryable<TResult>> GetQueryAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return await Task.FromResult(_dbSet.Where(predicate).Select(selector));
        }

        //public async Task InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        //{
        //    await _dbSet.AddRangeAsync(entities, cancellationToken);
        //    await _context.SaveChangesAsync();
        //}

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(a => a.ProductId == id);
        }

        public async Task UpdaetManyAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("实体集合不能为空", nameof(entities));
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T product, CancellationToken cancellationToken = default)
        {
            if(product != null)
            _dbSet.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                await UpdateAsync(entity);
            }
        }

        public async Task<T> GetByIdAsync(int id)
        {
#pragma warning disable CS8603 // 可能返回 null 引用。
            return await _dbSet.SingleOrDefaultAsync(e => e.ProductId == id);
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        #region 高效批量操作（EF Core 8新增）批量添加（高性能）
        public async Task<int> BulkInsertAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> BulkUpdateAsync(IEnumerable<T> entities)
        {
            var updateCount = 0;
            var entityType = _context.Model.FindEntityType(typeof(T));
            var primaryKey = entityType.FindPrimaryKey().Properties[0].Name;

            foreach (var item in entities.GroupBy(e => e.GetType().GetProperty(primaryKey)?.GetValue(e)))
            {
                var entity = item.First();
                var entry = _context.Entry(entity);
                await _dbSet
                    .Where(e => EF.Property<int>(e, primaryKey) == (int)item.Key)
                    .ExecuteUpdateAsync(setters =>
                    entry.Properties
                         .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                         .Aggregate(
                            setters,
                            (current, prop) => current.SetProperty(
                                p => EF.Property<object>(p, prop.Metadata.Name),
                                prop.CurrentValue
                                )
                            )
                         );
                updateCount += item.Count();
            }
            return updateCount;
        }

        public async Task<int> BulkSoftDeleteAsync(IEnumerable<int> ids, string deletedBy)
        {
            return await _dbSet.Where(e => ids.Contains(e.ProductId))
                .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.UpdatedDate, DateTime.UtcNow));
        }
        #endregion

    }
}
