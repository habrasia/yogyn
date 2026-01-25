using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<YogynDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<YogynDbContext>();
var conn = db.Database.GetDbConnection();

app.Logger.LogInformation("DB CONNECTED TO: {DataSource} | DB: {Database}",
    conn.DataSource, conn.Database);

app.Run();