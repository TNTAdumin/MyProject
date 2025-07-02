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

//����������ӷ�����������ӷ���
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
// ����ӳ�����
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<Product, CreateProductDto>()
       .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName));
});

// ע�᷺�Ͳִ�
builder.Services.AddScoped(typeof(IProductRepository<>), typeof(Repositories<>));
// ע�Ṥ����Ԫ
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//ע�����
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<ExcelHandler>();  // ע�������
builder.Services.AddScoped<IExcelService, ExcelHandler>();  // ע��ӿ�ӳ��

builder.Services.AddScoped<CsvHandler>();
builder.Services.AddScoped<IExcelService, CsvHandler>();
builder.Services.AddScoped<Func<string, IExcelService>>(serviceProvider => key =>
{
    // ֱ�Ӵ�������ȡ IExcelService ��ʵ��
    var services = serviceProvider.GetServices<IExcelService>();
    if (services == null || !services.Any())
        throw new InvalidOperationException("û��ע���κ� IExcelService ʵ�֣�");
    return key.ToLower() switch
    {
        "excel" => services.OfType<ExcelHandler>().FirstOrDefault()
                   ?? throw new InvalidOperationException("ExcelHandler δע��"),
        "csv" => services.OfType<CsvHandler>().FirstOrDefault()
                 ?? throw new InvalidOperationException("CsvHandler δע��"),
        _ => throw new NotImplementedException()
    };
});

// ����CORS���� - ����������������
builder.Services.AddCors(option =>
{
    // ������������Դ������
    option.AddPolicy("DevCorsPolicy", policy =>
    {
        //������������Դ������

        policy.WithOrigins(
            "http://localhost:3000",    // ���͵�React����������
            "http://localhost:4200",    // ���͵�Angular����������
            "http://localhost:8080",    // ���͵�Vue����������
            "https://localhost:7221"    // ��һ�����ܵ�ASP.NET Core�����˿�
            )
            .AllowAnyMethod()               // ��������HTTP����
            .AllowAnyHeader()               // ��������ͷ
            .AllowCredentials();            // ����ƾ��(��cookies)
    });
});

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseDeveloperExceptionPage();

    // �ڿ���������ʹ�ÿ��ɵ�CORS����
    app.UseCors("DevCorsPolicy");
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
// ����HTTP����ܵ�
app.UseRouting();
app.UseAuthorization();
// ����·��
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
