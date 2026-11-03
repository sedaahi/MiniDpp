using Microsoft.EntityFrameworkCore;
using MiniDpp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


var connectionString = builder.Configuration.GetConnectionString("MiniDpp")
                       ?? throw new InvalidOperationException("MiniDpp connection string is missing.");

builder.Services.AddDbContext<MiniDppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();