using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Resend;
using Yogyn.Api.Data;
using Yogyn.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Configure Key Vault
// ==========================================
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential()
    );
}

// ==========================================
// Configure Database
// ==========================================
var connectionString = builder.Configuration["db-connection-string"]
    ?? throw new InvalidOperationException("Database connection string not found");

builder.Services.AddDbContext<YogynDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==========================================
// Configure Email Services
// ==========================================
builder.Services.AddSingleton<EmailTemplateLoader>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();

// Configure Resend with HttpClient
builder.Services.AddHttpClient();

builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = builder.Configuration["resend-api-key"] ?? "";
});

builder.Services.AddScoped<IResend, ResendClient>();

// ==========================================
// Configure Service Bus
// ==========================================
builder.Services.AddSingleton<IMessageBusService, ServiceBusService>();

// ==========================================
// Configure Controllers & Swagger
// ==========================================
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// ==========================================
// Configure CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// Configure Background Workers
// ==========================================
builder.Services.AddHostedService<Yogyn.Api.Workers.BookingNotificationWorker>();

// ==========================================
// Build Application
// ==========================================
var app = builder.Build();

// ==========================================
// Configure Middleware
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();