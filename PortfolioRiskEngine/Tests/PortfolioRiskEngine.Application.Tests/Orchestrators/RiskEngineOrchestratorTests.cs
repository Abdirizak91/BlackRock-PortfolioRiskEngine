using Microsoft.Extensions.Logging;
using NSubstitute;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;
using PortfolioRiskEngine.Domain.Entities;
using PortfolioRiskEngine.Domain.Services;
using Shouldly;

namespace PortfolioRiskEngine.Application.Tests.Orchestrators;

public class RiskEngineOrchestratorTests
{
    private readonly ICsvReaderService _csvReaderService;
    private readonly IRiskCalculator _riskCalculator;
    private readonly IRiskResultRepository _riskResultRepository;
    private readonly RiskEngineOrchestrator _sut;

    public RiskEngineOrchestratorTests()
    {
        _csvReaderService = Substitute.For<ICsvReaderService>();
        _riskCalculator = Substitute.For<IRiskCalculator>();
        _riskResultRepository = Substitute.For<IRiskResultRepository>();
        _riskResultRepository.SaveScenarioResultAsync(Arg.Any<ScenarioResultDto>()).Returns(Result.Success());
        var logger = Substitute.For<ILogger<RiskEngineOrchestrator>>();
        _sut = new RiskEngineOrchestrator(_csvReaderService, _riskCalculator, _riskResultRepository, logger);
    }

    [Fact]
    public async Task CalculateRiskAsync_ShouldReturnPortfolioResult_WhenRequestIsValid()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m }
        };

        _csvReaderService.ReadPortfoliosAsync().Returns(new List<Portfolio>
        {
            new() { PortId = 1, PortName = "P1", PortCountry = "US", PortCcy = "USD" }
        });

        _csvReaderService.ReadLoansAsync().Returns(new List<Loan>
        {
            new() { LoanId = 10, PortId = 1, OutstandingAmount = 100m, CollateralValue = 200m, CreditRating = "A" }
        });

        _csvReaderService.ReadRatingsAsync().Returns(new List<Rating>
        {
            new() { RatingCode = "A", ProbabilityOfDefault = 0.05m }
        });

        _riskCalculator.CalculateScenarioCollateralValue(200m, -0.1m).Returns(180m);
        _riskCalculator.CalculateRecoveryRate(180m, 100m).Returns(1.8m);
        _riskCalculator.CalculateLossGivenDefault(1.8m).Returns(-0.8m);
        _riskCalculator.CalculateExpectedLoss(0.05m, -0.8m, 100m).Returns(-4m);

        var result = await _sut.CalculateRiskAsync(request);

        result.IsSuccess.ShouldBeTrue();

        await _riskResultRepository.Received(1).SaveScenarioResultAsync(Arg.Is<ScenarioResultDto>(saved =>
            saved.CountryPercentageChanges == request.CountryPercentageChanges &&
            saved.PortfolioResults.Count == 1 &&
            saved.PortfolioResults[0].PortfolioId == 1 &&
            saved.PortfolioResults[0].PortfolioName == "P1" &&
            saved.PortfolioResults[0].Country == "US" &&
            saved.PortfolioResults[0].Currency == "USD" &&
            saved.PortfolioResults[0].TotalOutstandingAmount == 100m &&
            saved.PortfolioResults[0].TotalCollateralValue == 200m &&
            saved.PortfolioResults[0].TotalScenarioCollateralValue == 180m &&
            saved.PortfolioResults[0].TotalExpectedLoss == -4m));
    }

    [Fact]
    public async Task CalculateRiskAsync_ShouldUseZeroPercentageChange_WhenCountryIsMissingFromRequest()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m }
        };

        _csvReaderService.ReadPortfoliosAsync().Returns(new List<Portfolio>
        {
            new() { PortId = 1, PortName = "P1", PortCountry = "UK", PortCcy = "GBP" }
        });

        _csvReaderService.ReadLoansAsync().Returns(new List<Loan>
        {
            new() { LoanId = 10, PortId = 1, OutstandingAmount = 120m, CollateralValue = 300m, CreditRating = "A" }
        });

        _csvReaderService.ReadRatingsAsync().Returns(new List<Rating>
        {
            new() { RatingCode = "A", ProbabilityOfDefault = 0.05m }
        });

        _riskCalculator.CalculateScenarioCollateralValue(Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(0m);
        _riskCalculator.CalculateRecoveryRate(Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(0m);
        _riskCalculator.CalculateLossGivenDefault(Arg.Any<decimal>()).Returns(1m);
        _riskCalculator.CalculateExpectedLoss(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(0m);

        await _sut.CalculateRiskAsync(request);

        _riskCalculator.Received(1).CalculateScenarioCollateralValue(300m, 0m);
    }

    [Fact]
    public async Task CalculateRiskAsync_ShouldReturnZeroTotals_WhenPortfolioHasNoLoans()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.2m }
        };

        _csvReaderService.ReadPortfoliosAsync().Returns(new List<Portfolio>
        {
            new() { PortId = 1, PortName = "P1", PortCountry = "US", PortCcy = "USD" }
        });

        _csvReaderService.ReadLoansAsync().Returns(new List<Loan>());
        _csvReaderService.ReadRatingsAsync().Returns(new List<Rating>());

        var result = await _sut.CalculateRiskAsync(request);

        result.IsSuccess.ShouldBeTrue();

        await _riskResultRepository.Received(1).SaveScenarioResultAsync(Arg.Is<ScenarioResultDto>(saved =>
            saved.PortfolioResults.Count == 1 &&
            saved.PortfolioResults[0].TotalOutstandingAmount == 0m &&
            saved.PortfolioResults[0].TotalCollateralValue == 0m &&
            saved.PortfolioResults[0].TotalScenarioCollateralValue == 0m &&
            saved.PortfolioResults[0].TotalExpectedLoss == 0m));

        _riskCalculator.DidNotReceive().CalculateScenarioCollateralValue(Arg.Any<decimal>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task CalculateRiskAsync_ShouldUseZeroProbabilityOfDefault_WhenRatingIsMissing()
    {
        var request = new ScenarioRequestDto
        {
            CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m }
        };

        _csvReaderService.ReadPortfoliosAsync().Returns(new List<Portfolio>
        {
            new() { PortId = 1, PortName = "P1", PortCountry = "US", PortCcy = "USD" }
        });

        _csvReaderService.ReadLoansAsync().Returns(new List<Loan>
        {
            new() { LoanId = 10, PortId = 1, OutstandingAmount = 100m, CollateralValue = 200m, CreditRating = "ZZ" }
        });

        _csvReaderService.ReadRatingsAsync().Returns(new List<Rating>());

        _riskCalculator.CalculateScenarioCollateralValue(Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(100m);
        _riskCalculator.CalculateRecoveryRate(Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(1m);
        _riskCalculator.CalculateLossGivenDefault(Arg.Any<decimal>()).Returns(0m);
        _riskCalculator.CalculateExpectedLoss(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(0m);

        await _sut.CalculateRiskAsync(request);

        _riskCalculator.Received(1).CalculateExpectedLoss(0m, 0m, 100m);
    }
}