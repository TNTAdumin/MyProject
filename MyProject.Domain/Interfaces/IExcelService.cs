using MyProject.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Domain.Interfaces
{
    public interface IExcelService
    {
        IAsyncEnumerable<Product> Import(Stream stream, bool hasHeader = true, CancellationToken cancellationToken = default);
        Task<Stream> Export(IEnumerable<Product> data);
    }
}
