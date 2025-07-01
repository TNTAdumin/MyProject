using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.IService;
using MyProject.Domain.Common;
using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;
using NPOI.Util;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Sockets;
using Npoi.Mapper;
using System.Runtime.CompilerServices;
using Azure.Messaging;
using System.Runtime.Serialization;
using MyProject.Infrastructure.Utilities;
using System.Diagnostics;
using NPOI.SS.UserModel;
using System.Linq.Expressions;
using LinqKit;
using NPOI.SS.Formula.Functions;

namespace MyProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly Func<string, IExcelService> _excelServiceFactory;
        private readonly IMapper _mapper;
        public ProductsController(IProductService productService, Func<string, IExcelService> excelServiceFactory, IMapper mapper)
        {
            _productService = productService;
            _excelServiceFactory = excelServiceFactory;
            _mapper = mapper;
        }

        [HttpGet("getById/{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("传入Id为空。");
            var data = await _productService.GetByIdAsync(id);
            if (data == null)
                return NotFound();
            return Ok(data);
        }

        [HttpGet("getPageList")]
        public async Task<ActionResult<PagedResultDto<ProductDto>>> GetPageList([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var pageList = await _productService.GetPagedListAsync(pageIndex, pageSize);
            return Ok(pageList);
        }

        [HttpGet("getProduct")]
        public async Task<ActionResult<ProductDto>> GetProductAsync([FromQuery] int Id, [FromQuery] string productName)
        {
            if (string.IsNullOrEmpty(Id.ToString()))
                return BadRequest("传入Id为空。");
            if (string.IsNullOrEmpty(productName))
                return BadRequest("传入商品名称为空。");
            var data = await _productService.GetProductAsync(Id, productName);
            if (data == null)
                return NotFound();
            return Ok(data);
        }

        [HttpPost("create")]
        public async Task<ActionResult<ProductDto>> CreateAsync([FromBody] CreateProductDto createProductDto)
        {
            if (createProductDto == null)
                return BadRequest("传入数据为空。");
            var products = await _productService.CreateAsync(createProductDto);
            //return CreatedAtAction(nameof(GetById), new { id = products.ProductId }, products);
            return Ok(new
            {
                Success = true,
                Message = "添加成功",
                //Data = products
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            if (updateProductDto == null)
                return BadRequest("传入数据为空。");
            if (await _productService.GetByIdAsync(id) == null)
                return BadRequest("传入Id不匹配。");
            await _productService.UpdateAsync(id, updateProductDto);
            //return NoContent();
            return Ok(new {
                Success = true,
                Message = "修改成功",
            });
        }

        /// <summary>
        /// 删除产品，如果产品不存在则返回404 Not Found；该方法会删除数据库的数据。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("传入Id为空。");
            if (await _productService.GetByIdAsync(id) == null)
                return NotFound("未找到产品。");
            await _productService.DeleteAsync(id);
            return Ok(new
            {
                Success = true,
                Message = "数据已从数据库删除成功",
            });
        }

        /// <summary>
        /// 软删除，如果产品不存在则返回404 Not Found；该方法不会删除数据库的数据，而是将数据标记为已删除。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("solt")]
        public async Task<IActionResult> SoftDeleteAsync(int id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("传入Id为空。");
            if (await _productService.GetByIdAsync(id) == null)
                return NotFound("未找到产品。");
            await _productService.SoftDeleteAsync(id);
            return Ok(new
            {
                Success = true,
                Message = "删除成功",
            });
        }

        /// <summary>
        /// 导入产品数据，支持Excel文件格式。文件流会被读取并解析为产品数据。
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)]// 限制上传文件大小为10MB
        public async Task<IActionResult> ImportProducts(IFormFile file, [FromQuery] bool hasHeader = true,
    CancellationToken cancellationToken = default)
        {
            try
            {
                //1.检查传入的文件是否为空或未选择
                if (file == null || file.Length == 0)
                    return BadRequest("上传的文件为空。");

                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("文件大小不能超过10MB");

                //2.根据文件类型选择合适的处理器
                var fileExtension = Path.GetExtension(file.FileName).ToLower();// 获取文件扩展名
                var excelService = fileExtension switch
                {
                    ".xlsx" => _excelServiceFactory("excel"), // Excel处理器
                    ".xls" => _excelServiceFactory("excel"), // Excel处理器
                    ".csv" => _excelServiceFactory("csv"), // CSV处理器
                    _ => throw new NotSupportedException("目前仅支持excle或csv文件格式。")
                };
                //3.创建导入统计对象
                var importResult = new ImportResult
                {
                    TotalProcessed = 0,
                    SuccessCount = 0,
                    ErrorMessages = new List<string>()
                };
                //4.读取文件流并导入数据
                await using var stream = file.OpenReadStream();// 打开文件流

                // 重置流位置（确保可以重复读取）
                stream.Position = 0;

                await foreach (var product in excelService.Import(stream, hasHeader).WithCancellation(cancellationToken))
                {
                    try
                    {
                        //5.验证产品数据
                        var validationResult = ValidateProduct(product);
                        if (!validationResult.IsVaild)
                        {
                            importResult.ErrorMessages.Add($"行 {importResult.TotalProcessed + 1}: {validationResult.ErrorMessage}");// 捕获验证错误
                            continue; // 跳过无效数据
                        }
                        //6.添加产品数据到数据库
                        await _productService.CreateAsync(_mapper.Map<CreateProductDto>(product));
                        importResult.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        importResult.ErrorMessages.Add($"行 {importResult.TotalProcessed + 1}: 处理时发生错误 - {ex.Message}"); // 捕获异常并记录错误信息
                    }
                    finally
                    {
                        importResult.TotalProcessed++; // 增加处理计数
                    }
                }
                return Ok(new
                {
                    Message = "产品导入完成",
                    Total = importResult.TotalProcessed,
                    Success = importResult.SuccessCount,
                    Errors = importResult.ErrorMessages,
                    ErrorCount = importResult.ErrorMessages.Count
                });
            }
            catch (NotSupportedException ex)
            {

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "产品导入失败");
                return StatusCode(500, "处理文件时发生内部错误");
            }
        }

        /// <summary>
        ///导出全部产品数据为Excel文件，返回文件流。后续可以添加分页参数和查询条件来控制导出数据量。
        /// </summary>
        /// <param name="query"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportProducts([FromQuery] ProductExportQuery query, [FromQuery] ExportOptions options = null)
        {
            try
            {
                //1.参数处理与验证
                options ??= new ExportOptions(); // 如果未传入选项，则使用默认值
                if (!ValidateExportOptions(options, out var validationResult))
                {
                    return BadRequest(validationResult);
                }

                //2.构建筛选条件
                var filer = BuildFilter(query);
                

                //3. 获取数据（带筛选条件）
                var stopwatch = Stopwatch.StartNew();// 记录导出时间
                var pagedata = await _productService.GetPagedListAsync(query.Page, query.PageSize); // 获取所有产品数据

                if (pagedata == null)
                    return NotFound("没有找到可导出的产品数据。");

                //3. 根据导出格式选择处理器
                var exportFormat = options.Format.ToLower() ?? "excel";// 默认导出格式为Excel
                var excelService = _excelServiceFactory(exportFormat); // 获取Excel处理器

                //4. 异步导出产品数据
                var data = pagedata.Items.Select(p => _mapper.Map<Product>(p)); // 将DTO转换为实体
                var stream = await excelService.Export(data); // 异步导出产品数据

                //5.性能指标记录
                //stopwatch.Stop();
                //_logger.LogInformation("产品数据导出完成，耗时 {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

                //6. 返回文件流，设置正确的MIME类型和文件名
                return CreateFileResult(stream, exportFormat, options.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "导出过程中发生错误");
            }
        }

        #region 导入导出辅助类和验证方法
        private class ImportResult
        {
            public int TotalProcessed { get; set; }
            public int SuccessCount { get; set; }
            public List<string> ErrorMessages { get; set; }
        }

        // 导出选项类
        public class ExportOptions
        {
            public string Format { get; set; } = "excel"; // excel/csv
            public string? FileName { get; set; }
            public int PageSize { get; set; } = 500;
            //public string[] Columns { get; set; } // 可选：指定导出的列
        }

        // 查询参数类
        public class ProductExportQuery
        {
            public string ProductName { get; set; }
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public int? MinStock { get; set; }
            public int? CategoryId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 1000;
        }

        private (bool IsVaild, string ErrorMessage) ValidateProduct(Product product)
        {
            if (product == null)
                return (false, "产品数据不能为空。");
            if (string.IsNullOrEmpty(product.ProductName))
                return (false, "产品名称不能为空。");
            if (product.Price <= 0)
                return (false, "产品价格必须大于0。");

            return (true, null);
        }

        private IActionResult CreateFileResult(Stream stream, string format, string customFileName = null)
        {
            // 默认文件名（当customFileName为null或空白时使用）
            string defaultFileName = $"产品数据_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            // 处理文件名
            string fileName = string.IsNullOrWhiteSpace(customFileName)
                ? defaultFileName
                : customFileName.Trim();

            // 确保文件名不包含非法字符
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            // 添加文件扩展名
            string fileExtension = format.ToLower() switch
            {
                "csv" => ".csv",
                _ => ".xlsx" // 默认Excel格式
            };

            // 组合完整文件名（避免重复添加扩展名）
            if (!fileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                fileName += fileExtension;
            }

            // 设置Content-Type
            string contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };


            if (stream.CanSeek)
                stream.Position = 0;

            return File(stream, contentType, fileName);
        }

        /// <summary>
        /// 导出数据限制
        /// </summary>
        /// <param name="options"></param>
        /// <param name="validationResult"></param>
        /// <returns></returns>
        private bool ValidateExportOptions(ExportOptions options, out object validationResult)
        {
            validationResult = null;

            if (options.PageSize > 1000)
            {
                validationResult = new { Message = "单次导出数量不能超过1000条" };
                return false;
            }
            return true;
        }

        // 筛选条件构建
        private Expression<Func<Product, bool>> BuildFilter(ProductExportQuery query)
        {
            var predicate = PredicateBuilder.New<Product>(true);

            if (!string.IsNullOrEmpty(query.ProductName))
            {
                predicate = predicate.And(p => p.ProductName.Contains(query.ProductName));
            }

            if (query.MinPrice.HasValue)
            {
                predicate = predicate.And(p => p.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                predicate = predicate.And(p => p.Price <= query.MaxPrice.Value);
            }

            if (query.MinStock.HasValue)
            {
                predicate = predicate.And(p => p.Stock >= query.MinStock.Value);
            }

            if (query.StartDate.HasValue)
            {
                predicate = predicate.And(p => p.CreatedDate >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                predicate = predicate.And(p => p.CreatedDate <= query.EndDate.Value);
            }

            return predicate;
        }
        #endregion


    }
}
