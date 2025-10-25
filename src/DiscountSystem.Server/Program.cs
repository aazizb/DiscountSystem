using DiscountSystem.Domain.Interfaces;
using DiscountSystem.Infrastructure;
using DiscountSystem.Infrastructure.Repositories;
using DiscountSystem.Server.Services;
using DiscountSystem.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Configure DbContext with retry logic
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var maxRetryCount = builder.Configuration.GetValue<int>("Database:MaxRetryCount", 10);
var maxRetryDelay = builder.Configuration.GetValue<int>("Database:MaxRetryDelay", 30);

builder.Services.AddDbContext<DiscountCodeDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: maxRetryCount,
            maxRetryDelay: TimeSpan.FromSeconds(maxRetryDelay),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);

        sqlOptions.MigrationsAssembly("DiscountSystem.Server");
    });
});

builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IDiscountCodeGenerator, DiscountCodeGenerator>();

// Enable HTTP/3 
int intPort = 5001; // Default port for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(intPort, listenOptions => 
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
});

var app = builder.Build();
app.UseHsts();
// apply migrations on startup (optional)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DiscountCodeDbContext>();
        db.Database.Migrate();
    }
}
// Configure the HTTP request pipeline.
app.MapGrpcService<DiscountCodeServices>();

app.Run();
