using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PortfolioRiskEngine.Infrastructure.Services;

namespace PortfolioRiskEngine.IntegrationTests.Infrastructure;

public sealed class IntegrationTestApiFactory : WebApplicationFactory<Program>
{
    public WebApplicationFactory<Program> WithSqliteConnectionString(string connectionString)
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<RiskResultsDatabaseOptions>(options =>
                {
                    options.ConnectionString = connectionString;
                });
            });
        });
    }
}

internal sealed class TemporarySqliteDatabase : IDisposable
{
    private readonly string _directory;

    private TemporarySqliteDatabase(string directory, string connectionString)
    {
        _directory = directory;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static TemporarySqliteDatabase Create()
    {
        var directory = Path.Combine(Path.GetTempPath(), "portfolio-risk-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var dbPath = Path.Combine(directory, "integration-risk-results.db");
        return new TemporarySqliteDatabase(directory, $"Data Source={dbPath}");
    }

    public void Dispose()
    {
        if (!Directory.Exists(_directory))
            return;

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(_directory, true);
                return;
            }
            catch (IOException)
            {
                if (attempt == maxAttempts)
                    return;

                Thread.Sleep(100 * attempt);
            }
            catch (UnauthorizedAccessException)
            {
                if (attempt == maxAttempts)
                    return;

                Thread.Sleep(100 * attempt);
            }
        }
    }
}




