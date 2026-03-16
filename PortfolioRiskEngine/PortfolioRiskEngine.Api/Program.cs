using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Domain.Services;
using PortfolioRiskEngine.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath,
    "..", "PortfolioRiskEngine.Infrastructure", "Data");

builder.Services.AddSingleton<ICsvReaderService>(new CsvReaderService(Path.GetFullPath(dataDirectory)));
builder.Services.AddSingleton<IRiskCalculator, RiskCalculator>();
builder.Services.AddScoped<IRiskEngineOrchestrator, RiskEngineOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactDev");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();