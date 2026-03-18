using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.IntegrationTests.Infrastructure;
using Shouldly;

namespace PortfolioRiskEngine.IntegrationTests;

public class RunsSearchEndpointIntegrationTests : IClassFixture<IntegrationTestApiFactory>
{
    private readonly IntegrationTestApiFactory _factory;

    public RunsSearchEndpointIntegrationTests(IntegrationTestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchRuns_ShouldReturnPagedData_WhenRunsExist()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var app = _factory.WithSqliteConnectionString(database.ConnectionString);
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var requestOne = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.10m }
        };

        var requestTwo = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["UK"] = 0.05m }
        };

        var saveOne = await client.PostAsJsonAsync("/RiskEngine/calculate-risk", requestOne);
        var saveTwo = await client.PostAsJsonAsync("/RiskEngine/calculate-risk", requestTwo);

        saveOne.StatusCode.ShouldBe(HttpStatusCode.Created);
        saveTwo.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await client.GetAsync("/RunsSearch/search-runs?pageNumber=1&pageSize=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SearchRunsResponseDto>();
        payload.ShouldNotBeNull();
        payload.PageNumber.ShouldBe(1);
        payload.PageSize.ShouldBe(1);
        payload.TotalCount.ShouldBe(2);
        payload.Runs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchRuns_ShouldReturn400BadRequest_WhenPaginationIsInvalid()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var app = _factory.WithSqliteConnectionString(database.ConnectionString);
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/RunsSearch/search-runs?pageNumber=0&pageSize=200");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("PageNumber must be >= 1 and PageSize must be between 1 and 100.");
    }
}
