using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;
using MyProject.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _myDbContext;
        public IProductRepository<Product> Product { get; }
        public UnitOfWork(MyDbContext myDbContext, IProductRepository<Product> products)
        {
            _myDbContext = myDbContext;
            Product = products;
        }
        public async Task<int> CompleteAsync() => await _myDbContext.SaveChangesAsync();
        public void Dispose() => _myDbContext.Dispose();
    }
}
