using MyProject.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Domain.Interfaces
{
    /**
     *工作单元接口 
     *IDisposable用来处理非托管资源
     *接口的对象应该在其生命周期结束时显式调用Dispose方法
     **/
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository<Product> Product { get; }
        Task<int> CompleteAsync();
    }
}
