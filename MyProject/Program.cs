using Microsoft.EntityFrameworkCore;
using MyProject.Application;
using MyProject.Application.DTOs;
using MyProject.Application.IService;
using MyProject.Application.Service;
using MyProject.Domain.Interfaces;
using MyProject.Domain.Models;
using MyProject.Infrastructure;
using MyProject.Infrastructure.Data;
using MyProject.Infrastructure.Service;

var builder = WebApplication.CreateBuilder(args);

//向容器中添加服务向容器添加服务
builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
// 配置映射规则
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<Product, CreateProductDto>()
       .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName));
});

// 注册泛型仓储
builder.Services.AddScoped(typeof(IProductRepository<>),typeof(Repositories<>));
// 注册工作单元
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//注册服务
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<ExcelHandler>();  // 注册具体类
builder.Services.AddScoped<IExcelService, ExcelHandler>();  // 注册接口映射

builder.Services.AddScoped<CsvHandler>();
builder.Services.AddScoped<IExcelService, CsvHandler>();
builder.Services.AddScoped<Func<string, IExcelService>>(serviceProvider => key =>
{
    // 直接从容器获取 IExcelService 的实现
    var services = serviceProvider.GetServices<IExcelService>();
    if (services == null || !services.Any())
        throw new InvalidOperationException("没有注册任何 IExcelService 实现！");
    return key.ToLower() switch
    {
        "excel" => services.OfType<ExcelHandler>().FirstOrDefault()
                   ?? throw new InvalidOperationException("ExcelHandler 未注册"),
        "csv" => services.OfType<CsvHandler>().FirstOrDefault()
                 ?? throw new InvalidOperationException("CsvHandler 未注册"),
        _ => throw new NotImplementedException()
    };
});

//跨域
builder.Services.AddCors(option =>
{
    option.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
// 配置HTTP请求管道
app.UseRouting();
app.UseAuthorization();
// 配置路由
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
