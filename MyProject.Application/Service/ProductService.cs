using AutoMapper;
using MyProject.Application.DTOs;
using MyProject.Application.IService;
using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;

namespace MyProject.Application.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository<Product> _product;
        private readonly IExcelService _excelService;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository<Product> productRepository, IExcelService excelService, IMapper mapper)
        {
            _product = productRepository;
            _excelService = excelService;
            _mapper = mapper;
        }

        public async Task<int> BulkInsertAsync(IEnumerable<CreateProductDto> entities)
        {
            var products = _mapper.Map<IEnumerable<Product>>(entities);
            return await _product.BulkInsertAsync(products);
        }

        public async Task<int> BulkSoftDeleteAsync(IEnumerable<int> ids, string deletedBy)
        {
            return await _product.BulkSoftDeleteAsync(ids, deletedBy);
        }

        public async Task<int> BulkUpdateAsync(IEnumerable<UpdateProductDto> entities)
        {
            var product = _mapper.Map<IEnumerable<Product>>(entities);
            if (product == null)
                throw new KeyNotFoundException($"未找到产品。");
            return await _product.BulkUpdateAsync(product);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto createProductDto)
        {
            var product = _mapper.Map<Product>(createProductDto);
            await _product.AddAsync(product);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _product.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("未找到产品。");
            await _product.DeleteAsync(id);
        }

        public async Task<Stream> ExportProducts(IEnumerable<Product> data)
        {
            return await _excelService.Export(data);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _product.GetAllAsync();
        }

        public async Task<ProductDto> GetByIdAsync(int id)
        {
            var product = await _product.GetByIdAsync(id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<PagedResultDto<ProductDto>> GetPagedListAsync(int pageIndex, int pageSize)
        {
            var pageList = await _product.GetPagedListAsync(pageIndex, pageSize);
            return new PagedResultDto<ProductDto>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = pageList.TotalCount,
                TotalPages = pageList.TotalPages,
                HasNextPage = pageList.HasNextPage,
                HasPreviousPage = pageList.HasPreviousPage,
                Items = _mapper.Map<IEnumerable<ProductDto>>(pageList.Items)
            };
        }

        public async Task<ProductDto> GetProductAsync(int Id, string productName)
        {
            var product = await _product.GetProductAsync(Id, productName);
            if (product == null)
                throw new KeyNotFoundException("未找到产品。");
            return _mapper.Map<ProductDto>(product);
        }

        public async IAsyncEnumerable<ProductDto> ImportProducts(Stream fileStream)
        {
            var products = _excelService.Import(fileStream);
            await foreach (var item in products)
            {
                await _product.AddAsync(item);
                yield return _mapper.Map<ProductDto>(item);
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            var product = await _product.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("未找到产品。");
            await _product.SoftDeleteAsync(id);
        }
        public async Task UpdateAsync(int id, UpdateProductDto updateProductDto)
        {
            var product = await _product.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("找不到id为｛id｝的产品。");
            _mapper.Map(updateProductDto, product);
            await _product.UpdateAsync(product);
        }
    }
}
