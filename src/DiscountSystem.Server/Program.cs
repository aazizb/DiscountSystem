using DiscountSystem.Domain.Interfaces;
using DiscountSystem.Infrastructure;
using DiscountSystem.Infrastructure.Repositories;
using DiscountSystem.Server.Services;
using DiscountSystem.Services;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddDbContext<DiscountCodeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    o => o.MigrationsAssembly("DiscountSystem.Server")));

builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IDiscountCodeGenerator, DiscountCodeGenerator>();

// Enable HTTP/3 
int intPort = 5001; // Default port for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(intPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps(); // HTTP/3 requires HTTPS
    });
});

var app = builder.Build();

// apply migrations on startup (optional)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DiscountCodeDbContext>();
    db.Database.Migrate();
}
// Configure the HTTP request pipeline.
app.MapGrpcService<DiscountCodeServices>();

app.Run();
