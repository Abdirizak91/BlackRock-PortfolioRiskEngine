using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.IntegrationTests.Infrastructure;
using Shouldly;

namespace PortfolioRiskEngine.IntegrationTests;

public class RiskEngineEndpointIntegrationTests : IClassFixture<IntegrationTestApiFactory>
{
    private readonly IntegrationTestApiFactory _factory;

    public RiskEngineEndpointIntegrationTests(IntegrationTestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CalculateRisk_ShouldReturn201Created_WhenRequestIsValid()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var app = _factory.WithSqliteConnectionString(database.ConnectionString);
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal>
            {
                ["US"] = -0.10m,
                ["UK"] = 0.05m
            }
        };

        var response = await client.PostAsJsonAsync("/RiskEngine/calculate-risk", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CalculateRisk_ShouldReturn400BadRequest_WhenCountryChangesAreEmpty()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var app = _factory.WithSqliteConnectionString(database.ConnectionString);
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal>()
        };

        var response = await client.PostAsJsonAsync("/RiskEngine/calculate-risk", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("At least one country percentage change is required.");
    }
}
