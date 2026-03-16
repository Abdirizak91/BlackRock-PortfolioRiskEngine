using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortfolioRiskEngine.Api.Controllers;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;
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
    public async Task CalculateRisk_ShouldReturnBadRequest_WhenNoCountryChangesProvided()
    {
        var request = new ScenarioRequestDto { CountryPercentageChanges = new Dictionary<string, decimal>() };

        var result = await _sut.CalculateRisk(request);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("At least one country percentage change is required.");
        await _riskEngineOrchestratorMock.DidNotReceive().CalculateRiskAsync(Arg.Any<ScenarioRequestDto>());
    }

    [Fact]
    public async Task CalculateRisk_ShouldReturnOkWithScenarioResult_WhenRequestIsValid()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m }
        };

        var expected = new ScenarioResultDto
        {
            RunDate = DateTime.UtcNow,
            TimeTakenMs = 12,
            CountryPercentageChanges = request.CountryPercentageChanges,
            PortfolioResults = []
        };

        _riskEngineOrchestratorMock
            .CalculateRiskAsync(request)
            .Returns(expected);

        var result = await _sut.CalculateRisk(request);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeSameAs(expected);
        await _riskEngineOrchestratorMock.Received(1).CalculateRiskAsync(request);
    }
}