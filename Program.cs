using alpha.Repositories;
using alpha.Services;
using alpha.Services.Bsc;
using alpha.Services.Sol;
using alpha.Services.Sui;
using alpha.Models;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.WriteIndented = true);

// Persistence
builder.Services.AddSingleton<iRepo, Repo>();

// Configuration
builder.Services.Configure<GlobalBlockchainConfig>(builder.Configuration.GetSection("BlockchainConfig"));

// Individual Chain Transfer Services
builder.Services.AddSingleton<IChainTransferService, BscTransfer>();
builder.Services.AddSingleton<IChainTransferService, SolTransfer>();
builder.Services.AddSingleton<IChainTransferService, SuiTransfer>();

// Individual Chain Balance Services
builder.Services.AddSingleton<IChainBalanceService, BscBalance>();
builder.Services.AddSingleton<IChainBalanceService, SolBalance>();
builder.Services.AddSingleton<IChainBalanceService, SuiBalance>();

// Unified Services
builder.Services.AddSingleton<ITransferService, TransferService>();
builder.Services.AddSingleton<IWalletService, WalletService>();

var app = builder.Build();

// Setup API Key Middleware
app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
        options.RoutePrefix = "swagger";
        options.DefaultModelsExpandDepth(-1);
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    app.MapGet("/", () => Results.Json(new { message = "Welcome", status = "ok", version = "1.0.0" }));
}

app.UseAuthorization();
app.MapControllers();
app.Run();
