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

    public async Task<Result<SearchRunsResponseDto>> SearchScenarioResultsAsync(SearchRunsRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            logger.LogError("{Section}:ConnectionString is missing or empty. Risk run search is skipped.", RiskResultsDatabaseOptions.SectionName);
            return Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.PersistenceConfigurationMissing());
        }

        try
        {
            EnsureDatabaseDirectoryExists();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await EnsureSchemaAsync(connection);

            var totalCount = await connection.ExecuteScalarAsync<int>(RiskResultsSql.SelectRiskRunsCount);

            if (totalCount == 0)
            {
                return Result<SearchRunsResponseDto>.Success(new SearchRunsResponseDto
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = 0,
                    Runs = []
                });
            }

            var offset = (request.PageNumber - 1) * request.PageSize;

            var runRows = (await connection.QueryAsync<RiskRunRow>(
                RiskResultsSql.SelectRiskRunsPage,
                new { Limit = request.PageSize, Offset = offset })).ToList();

            if (runRows.Count == 0)
            {
                return Result<SearchRunsResponseDto>.Success(new SearchRunsResponseDto
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    Runs = []
                });
            }

            var runIds = runRows.Select(run => run.RiskRunId).ToArray();

            var countryRows = (await connection.QueryAsync<CountryChangeRow>(
                RiskResultsSql.SelectCountryChangesByRunIds,
                new { RunIds = runIds })).ToList();

            var portfolioRows = (await connection.QueryAsync<PortfolioResultRow>(
                RiskResultsSql.SelectPortfolioResultsByRunIds,
                new { RunIds = runIds })).ToList();

            var runs = runRows.Select(run => new ScenarioResultDto
            {
                RunDate = run.RunDateUtc,
                TimeTakenMs = run.TimeTakenMs,
                CountryPercentageChanges = countryRows
                    .Where(country => country.RiskRunId == run.RiskRunId)
                    .ToDictionary(country => country.CountryCode, country => country.PercentageChange),
                PortfolioResults = portfolioRows
                    .Where(portfolio => portfolio.RiskRunId == run.RiskRunId)
                    .Select(portfolio => new PortfolioRiskResultDto
                    {
                        PortfolioId = portfolio.PortfolioId,
                        PortfolioName = portfolio.PortfolioName,
                        Country = portfolio.Country,
                        Currency = portfolio.Currency,
                        TotalOutstandingAmount = portfolio.TotalOutstandingAmount,
                        TotalCollateralValue = portfolio.TotalCollateralValue,
                        TotalScenarioCollateralValue = portfolio.TotalScenarioCollateralValue,
                        TotalExpectedLoss = portfolio.TotalExpectedLoss
                    })
                    .ToList()
            }).ToList();

            return Result<SearchRunsResponseDto>.Success(new SearchRunsResponseDto
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Runs = runs
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search persisted risk runs from SQLite.");
            return Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.PersistenceFailed());
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

    private sealed record RiskRunRow
    {
        public long RiskRunId { get; init; }
        public DateTime RunDateUtc { get; init; }
        public long TimeTakenMs { get; init; }
    }

    private sealed record CountryChangeRow
    {
        public long RiskRunId { get; init; }
        public string CountryCode { get; init; } = string.Empty;
        public decimal PercentageChange { get; init; }
    }

    private sealed record PortfolioResultRow
    {
        public long RiskRunId { get; init; }
        public int PortfolioId { get; init; }
        public string PortfolioName { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public decimal TotalOutstandingAmount { get; init; }
        public decimal TotalCollateralValue { get; init; }
        public decimal TotalScenarioCollateralValue { get; init; }
        public decimal TotalExpectedLoss { get; init; }
    }
}