using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Common;
using MyProject.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Domain.Interfaces
{
    /**
     *仓储接口 
     **/
    public interface IProductRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// 根据条件获取数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productName"></param>
        /// <param name="cancellationToken">允许一个任务或操作在外部被请求取消，并且任务本身会定期检查是否被取消请求，从而实现优雅终止</param>
        /// <returns></returns>
        Task<T> GetProductAsync(int id,string productName, CancellationToken cancellationToken = default);
        /// <summary>
        /// 查询全部数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPagedList<T>> GetPagedListAsync(int pageIndex, int pageSize);
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddAsync(T product, CancellationToken cancellationToken = default);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateAsync(T product, CancellationToken cancellationToken = default);
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task UpdaetManyAsync(IEnumerable<T> entities);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        /// <summary>
        /// 获取到IQueryable
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<IQueryable<T>> GetQueryAsync<TResult>(Expression<Func<T, bool>> predicate);

        Task<IQueryable<TResult>> GetQueryAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector);
        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(int id);
        /// <summary>
        /// 软删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task SoftDeleteAsync(int id);
        Task<T> GetByIdAsync(int id);
        Task<int> BulkInsertAsync(IEnumerable<T> entities);
        Task<int> BulkUpdateAsync(IEnumerable<T> entities);
        Task<int> BulkSoftDeleteAsync(IEnumerable<int> ids, string deletedBy);
    }
    //public class PagedList<T>
    //{
    //    public List<T> Items { get; set; }
    //    public int TotalCount { get; set; }
    //    public int PageNumber { get; set; }
    //    public int PageSize { get; set; }
    //    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    //}
}
