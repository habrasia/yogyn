using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Key Vault
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential()
    );
}

// Configure Database
var connectionString = builder.Configuration["db-connection-string"]
    ?? throw new InvalidOperationException("Database connection string not found");

builder.Services.AddDbContext<YogynDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Services
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

// Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();