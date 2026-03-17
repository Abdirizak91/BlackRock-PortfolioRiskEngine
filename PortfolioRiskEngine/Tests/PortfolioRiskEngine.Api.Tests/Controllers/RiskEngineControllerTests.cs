using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortfolioRiskEngine.Api.Controllers;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;
using Shouldly;

namespace PortfolioRiskEngine.Api.Tests.Controllers;

public class RiskEngineControllerTests
{
    private readonly IRiskEngineOrchestrator _riskEngineOrchestratorMock;
    private readonly RiskEngineController _sut;

    public RiskEngineControllerTests()
    {
        _riskEngineOrchestratorMock = Substitute.For<IRiskEngineOrchestrator>();
        var logger = Substitute.For<ILogger<RiskEngineController>>();
        _sut = new RiskEngineController(_riskEngineOrchestratorMock, logger);
    }

    [Fact]
    public async Task CalculateRisk_ShouldReturn400BadRequest_WhenNoCountryChangesProvided()
    {
        var request = new ScenarioRequestDto { CountryPercentageChanges = new Dictionary<string, decimal>() };

        _riskEngineOrchestratorMock
            .CalculateRiskAsync(request)
            .Returns(Result.Failure(RiskEngineErrors.InvalidCountryChanges()));

        var result = await _sut.CalculateRisk(request);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("At least one country percentage change is required.");
        await _riskEngineOrchestratorMock.Received(1).CalculateRiskAsync(request);
    }

    [Fact]
    public async Task CalculateRisk_ShouldReturn201Created_WhenRequestIsValid()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m }
        };

        _riskEngineOrchestratorMock
            .CalculateRiskAsync(request)
            .Returns(Result.Success());

        var result = await _sut.CalculateRisk(request);

        result.ShouldBeOfType<CreatedResult>();

        await _riskEngineOrchestratorMock.Received(1).CalculateRiskAsync(request);
    }
}