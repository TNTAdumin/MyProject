using MyProject.Application.DTOs;
using MyProject.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Application.IService
{
    public interface IProductService
    {
        Task<ProductDto> GetByIdAsync(int id);
        Task<ProductDto> GetProductAsync(int Id, string productName);
        Task<PagedResultDto<ProductDto>> GetPagedListAsync(int pageIndex, int pageSize);
        Task<ProductDto> CreateAsync(CreateProductDto createProductDto);
        Task UpdateAsync(int id, UpdateProductDto updateProductDto);
        Task DeleteAsync(int id);
        Task SoftDeleteAsync(int id);
        IAsyncEnumerable<ProductDto> ImportProducts(Stream fileStream);
        Task<Stream> ExportProducts(IEnumerable<Product> data);
        Task<int> BulkInsertAsync(IEnumerable<CreateProductDto> entities);
        Task<int> BulkUpdateAsync(IEnumerable<UpdateProductDto> entities);
        Task<int> BulkSoftDeleteAsync(IEnumerable<int> ids, string deletedBy);
        Task<IEnumerable<Product>> GetAllAsync();
    }
}
