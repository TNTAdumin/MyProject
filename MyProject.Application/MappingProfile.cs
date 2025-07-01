using AutoMapper;
using MyProject.Application.DTOs;
using MyProject.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Application
{
    /**
     *AutoMapper配置 
    **/
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<ProductDto, Product>();
            CreateMap<Product, ProductDto>();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
