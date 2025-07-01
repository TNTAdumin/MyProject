using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;
using Npoi.Mapper;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Infrastructure.Service
{
    // Excel处理器（NPOI实现）
    public class ExcelHandler : IExcelService
    {
        /// <summary>
        /// 导出数据到Excel文件
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<Stream> Export(IEnumerable<Product> data)
        {
            var workBook = new SXSSFWorkbook(1024); // 启用磁盘缓存
            try
            {
                var sheet = workBook.CreateSheet();
                int rowIndex = 0;

                // 添加标题头（加粗样式）
                var headerStyle = workBook.CreateCellStyle();
                var headerFont = workBook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                var headerRow = sheet.CreateRow(rowIndex++);
                for (int i = 0; i < 5; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.CellStyle = headerStyle;
                }

                headerRow.GetCell(0).SetCellValue("商品编号");
                headerRow.GetCell(1).SetCellValue("商品名称");
                headerRow.GetCell(2).SetCellValue("商品描述");
                headerRow.GetCell(3).SetCellValue("价格");
                headerRow.GetCell(4).SetCellValue("库存");
                //headerRow.GetCell(5).SetCellValue("创建时间");
                //headerRow.GetCell(6).SetCellValue("修改时间");
                //添加数据行
                foreach (var product in data)
                {
                    var row = sheet.CreateRow(rowIndex++);
                    row.CreateCell(0).SetCellValue(product.ProductId);
                    row.CreateCell(1).SetCellValue(product.ProductName);
                    row.CreateCell(2).SetCellValue(product.Description);
                    row.CreateCell(3).SetCellValue((double)product.Price);
                    row.CreateCell(4).SetCellValue(product.Stock);
                    //row.CreateCell(5).SetCellValue(product.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    //row.CreateCell(6).SetCellValue(product.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                }
                var tempStream = new MemoryStream();
                workBook.Write(tempStream); // 写入临时流（会被自动关闭）

                // 创建新流返回（确保未被关闭）
                var outputStream = new MemoryStream(tempStream.ToArray());
                outputStream.Position = 0;
                return outputStream;
            }
            finally
            {
                workBook.Dispose(); // 清理临时文件
            }
        }
        /// <summary>
        /// 从Excel文件流中导入数据
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Product> Import(Stream stream, bool hasHeader = true, CancellationToken cancellationToken = default)
        {
            using var workbook = new XSSFWorkbook(stream);// 从流中创建工作簿

            // 检查是否存在工作表
            if (workbook.NumberOfSheets == 0)
                throw new InvalidOperationException("Excel文件不包含任何工作表");

            var sheet = workbook.GetSheet("Sheet0") ?? workbook.GetSheetAt(0);// 获取第一个工作表
            //如果存在标题行则跳过，如果不期望标题，则设置为false
            var rowEnumerator = sheet.GetRowEnumerator();

            while (rowEnumerator.MoveNext()) 
            {
                if(hasHeader)
                {
                    hasHeader = false;
                    continue;
                }

                var row = (XSSFRow)rowEnumerator.Current;// 获取当前行
                if (row == null) continue; // 跳过空行

                yield return new Product
                {
                    //ProductId = (int)row.GetCell(0).NumericCellValue,
                    ProductName = row.GetCell(1).StringCellValue,
                    Description = row.GetCell(2).StringCellValue,
                    Price = (decimal)row.GetCell(3).NumericCellValue,
                    Stock = (int)row.GetCell(4).NumericCellValue,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    IsDeleted = false // 默认未删除
                };
            }
        }
    }
    // CSV处理器（流式读写）
    public class CsvHandler : IExcelService
    {
        /// <summary>
        /// 导出数据到CSV文件流
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<Stream> Export(IEnumerable<Product> data)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream,Encoding.UTF8,leaveOpen:true);
            try 
            {
                // 写入CSV头部
                await writer.WriteLineAsync("ProductId,ProductName,Description,Price,Stock");
                // 写入数据行
                foreach (var item in data)
                {
                    await writer.WriteLineAsync($"{item.ProductId},\"{item.ProductName}\",{item.Description}\",{item.Price}\",{item.Stock}");
                }
                await writer.FlushAsync();// 确保所有数据写入流
                stream.Position = 0;// 重置流位置
                return stream;
            }
            finally
            {
                writer.Dispose();
            }
        }
        /// <summary>
        /// 从CSV文件流中导入数据
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Product> Import(Stream stream, bool hasHeader = true, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(stream);
            //bool isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = (await reader.ReadLineAsync())?.Trim();// 去除首尾空格
                if(string.IsNullOrEmpty(line))continue; // 跳过空行
                if (hasHeader)
                {
                    hasHeader = false; // 跳过标题行
                    continue;
                }
                var values = ParseCsvLine(line);// 分割CSV行


                yield return new Product
                {
                    //ProductId = int.Parse(values[0]),
                    ProductName = values[1],
                    Description = values[2],
                    Price = decimal.Parse(values[3]),
                    Stock = int.Parse(values[4]),
                };
            }
        }
        /// <summary>
        /// 解析CSV行，处理引号和逗号分隔符
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentValue = new StringBuilder();
            // 逐字符解析CSV行
            foreach (var item in line)
            {
                // 检查引号状态
                if (item == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (item == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else 
                {
                    currentValue.Append(item);
                }
            
            }
            result.Add(currentValue.ToString());// 添加最后一个值
            return result.ToArray();
        }
    }
}

