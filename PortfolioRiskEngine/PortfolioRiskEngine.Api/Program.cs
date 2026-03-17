using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Domain.Services;
using PortfolioRiskEngine.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath,
    "..", "PortfolioRiskEngine.Infrastructure", "Data");
var localRiskResultsDbPath = Path.Combine(Path.GetFullPath(dataDirectory), "risk-results.db");

builder.Services.Configure<RiskResultsDatabaseOptions>(
    builder.Configuration.GetSection(RiskResultsDatabaseOptions.SectionName));

builder.Services.PostConfigure<RiskResultsDatabaseOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.ConnectionString))
        options.ConnectionString = $"Data Source={localRiskResultsDbPath}";
});

builder.Services.AddSingleton<ICsvReaderService>(new CsvReaderService(Path.GetFullPath(dataDirectory)));
builder.Services.AddSingleton<IRiskCalculator, RiskCalculator>();
builder.Services.AddSingleton<IRiskResultRepository, RiskResultsRepository>();
builder.Services.AddScoped<IRiskEngineOrchestrator, RiskEngineOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unexpected server error",
            detail: exception?.Message)
            .ExecuteAsync(context);
    });
});

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