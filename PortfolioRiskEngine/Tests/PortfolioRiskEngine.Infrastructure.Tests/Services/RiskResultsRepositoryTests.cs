using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Results;
using PortfolioRiskEngine.Infrastructure.Services;
using Shouldly;

namespace PortfolioRiskEngine.Infrastructure.Tests.Services;

public class RiskResultsRepositoryTests
{
    private readonly SqliteFixture _fixture = CreateFixture();
    private readonly RiskResultsRepository _sut;

    public RiskResultsRepositoryTests()
    {
        var options = Options.Create(new RiskResultsDatabaseOptions { ConnectionString = _fixture.ConnectionString });
        _sut = new RiskResultsRepository(options, NullLogger<RiskResultsRepository>.Instance);
    }

    [Fact]
    public async Task SaveScenarioResultAsync_ShouldPersistRunCountryChangesAndPortfolioResults()
    {
        var result = new ScenarioResultDto
        {
            RunDate = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc),
            TimeTakenMs = 42,
            CountryPercentageChanges = new Dictionary<string, decimal>
            {
                ["US"] = -0.1m,
                ["UK"] = 0.05m
            },
            PortfolioResults =
            [
                new PortfolioRiskResultDto
                {
                    PortfolioId = 1,
                    PortfolioName = "P1",
                    Country = "US",
                    Currency = "USD",
                    TotalOutstandingAmount = 100m,
                    TotalCollateralValue = 140m,
                    TotalScenarioCollateralValue = 126m,
                    TotalExpectedLoss = 2m
                }
            ]
        };

        var persisted = await _sut.SaveScenarioResultAsync(result);
        persisted.IsSuccess.ShouldBeTrue();

        await using var connection = new SqliteConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var runCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_runs;");
        var countryCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_run_country_changes;");
        var portfolioCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_run_portfolio_results;");

        runCount.ShouldBe(1);
        countryCount.ShouldBe(2);
        portfolioCount.ShouldBe(1);
    }

    [Fact]
    public async Task SaveScenarioResultAsync_ShouldPersistOnlyRun_WhenResultContainsNoChildren()
    {
        var result = new ScenarioResultDto
        {
            RunDate = DateTime.UtcNow,
            TimeTakenMs = 8,
            CountryPercentageChanges = new Dictionary<string, decimal>(),
            PortfolioResults = []
        };

        var persisted = await _sut.SaveScenarioResultAsync(result);
        persisted.IsSuccess.ShouldBeTrue();

        await using var connection = new SqliteConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var runCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_runs;");
        var countryCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_run_country_changes;");
        var portfolioCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM risk_run_portfolio_results;");

        runCount.ShouldBe(1);
        countryCount.ShouldBe(0);
        portfolioCount.ShouldBe(0);
    }

    [Fact]
    public async Task SearchScenarioResultsAsync_ShouldReturnSavedRunsForRequestedPage()
    {
        var first = new ScenarioResultDto
        {
            RunDate = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc),
            TimeTakenMs = 11,
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m },
            PortfolioResults =
            [
                new PortfolioRiskResultDto
                {
                    PortfolioId = 1,
                    PortfolioName = "P1",
                    Country = "US",
                    Currency = "USD",
                    TotalOutstandingAmount = 100m,
                    TotalCollateralValue = 120m,
                    TotalScenarioCollateralValue = 108m,
                    TotalExpectedLoss = 1.5m
                }
            ]
        };

        var second = new ScenarioResultDto
        {
            RunDate = new DateTime(2026, 3, 18, 12, 0, 0, DateTimeKind.Utc),
            TimeTakenMs = 9,
            CountryPercentageChanges = new Dictionary<string, decimal> { ["UK"] = 0.05m },
            PortfolioResults = []
        };

        (await _sut.SaveScenarioResultAsync(first)).IsSuccess.ShouldBeTrue();
        (await _sut.SaveScenarioResultAsync(second)).IsSuccess.ShouldBeTrue();

        var request = new SearchRunsRequestDto { PageNumber = 1, PageSize = 1 };

        var result = await _sut.SearchScenarioResultsAsync(request);

        result.IsSuccess.ShouldBeTrue();
        var response = result.Value!;
        response.PageNumber.ShouldBe(1);
        response.PageSize.ShouldBe(1);
        response.TotalCount.ShouldBe(2);
        response.Runs.Count.ShouldBe(1);
        response.Runs[0].RunDate.ShouldBe(second.RunDate);

        var pageTwoRequest = new SearchRunsRequestDto { PageNumber = 2, PageSize = 1 };
        var pageTwoResult = await _sut.SearchScenarioResultsAsync(pageTwoRequest);

        pageTwoResult.IsSuccess.ShouldBeTrue();
        var pageTwoResponse = pageTwoResult.Value!;

        pageTwoResponse.TotalCount.ShouldBe(2);
        pageTwoResponse.Runs.Count.ShouldBe(1);
        pageTwoResponse.Runs[0].RunDate.ShouldBe(first.RunDate);
        pageTwoResponse.Runs[0].PortfolioResults.Count.ShouldBe(1);
        pageTwoResponse.Runs[0].CountryPercentageChanges["US"].ShouldBe(-0.1m);
    }

    private static SqliteFixture CreateFixture()
    {
        var dbDirectory = Path.Combine(Path.GetTempPath(), "portfolio-risk-db-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dbDirectory);

        var dbPath = Path.Combine(dbDirectory, "risk-results-tests.db");
        var connectionString = $"Data Source={dbPath}";

        return new SqliteFixture(dbDirectory, connectionString);
    }

    private sealed record SqliteFixture(string DirectoryPath, string ConnectionString) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}
