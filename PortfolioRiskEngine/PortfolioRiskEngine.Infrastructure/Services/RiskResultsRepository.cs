using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Results;
using PortfolioRiskEngine.Infrastructure.Helpers;

namespace PortfolioRiskEngine.Infrastructure.Services;

public class RiskResultsRepository(
    IOptions<RiskResultsDatabaseOptions> options,
    ILogger<RiskResultsRepository> logger) : IRiskResultRepository
{
    private readonly string _connectionString = options.Value.ConnectionString.Trim();

    public async Task<Result> SaveScenarioResultAsync(ScenarioResultDto result)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            logger.LogError("{Section}:ConnectionString is missing or empty. Risk result persistence is skipped.", RiskResultsDatabaseOptions.SectionName);
            return Result.Failure(RiskEngineErrors.PersistenceConfigurationMissing());
        }

        try
        {
            EnsureDatabaseDirectoryExists();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await EnsureSchemaAsync(connection);

            await using var transaction = await connection.BeginTransactionAsync();

            var riskRunId = await connection.ExecuteScalarAsync<long>(
                RiskResultsSql.InsertRiskRun,
                new
                {
                    RunDateUtc = result.RunDate,
                    result.TimeTakenMs,
                    CreatedUtc = DateTime.UtcNow
                },
                transaction);

            if (result.CountryPercentageChanges.Count > 0)
            {
                var countryRows = result.CountryPercentageChanges.Select(countryChange => new
                {
                    RiskRunId = riskRunId,
                    CountryCode = countryChange.Key,
                    PercentageChange = countryChange.Value
                });

                await connection.ExecuteAsync(RiskResultsSql.InsertCountryChange, countryRows, transaction);
            }

            if (result.PortfolioResults.Count > 0)
            {
                var portfolioRows = result.PortfolioResults.Select(portfolio => new
                {
                    RiskRunId = riskRunId,
                    portfolio.PortfolioId,
                    portfolio.PortfolioName,
                    portfolio.Country,
                    portfolio.Currency,
                    portfolio.TotalOutstandingAmount,
                    portfolio.TotalCollateralValue,
                    portfolio.TotalScenarioCollateralValue,
                    portfolio.TotalExpectedLoss
                });

                await connection.ExecuteAsync(RiskResultsSql.InsertPortfolioResult, portfolioRows, transaction);
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist risk run result into SQLite.");
            return Result.Failure(RiskEngineErrors.PersistenceFailed());
        }
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection)
    {
        await connection.ExecuteAsync(RiskResultsSql.CreateRiskRunsTable);
        await connection.ExecuteAsync(RiskResultsSql.CreateCountryChangesTable);
        await connection.ExecuteAsync(RiskResultsSql.CreatePortfolioResultsTable);
    }

    private void EnsureDatabaseDirectoryExists()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
            return;

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}