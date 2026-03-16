using PortfolioRiskEngine.Domain.Services;
using Shouldly;

namespace PortfolioRiskEngine.Domain.Tests.Services;

public class RiskCalculatorTests
{
    private readonly RiskCalculator _sut = new();

    [Theory]
    [InlineData(200, -0.1, -20)]
    [InlineData(0, -0.25, 0)]
    [InlineData(100, 1.2, 120)]
    public void CalculateScenarioCollateralValue_ShouldMultiplyInputs(decimal collateralValue, decimal percentageChange, decimal expected)
    {
        var result = _sut.CalculateScenarioCollateralValue(collateralValue, percentageChange);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(180, 100, 1.8)]
    [InlineData(0, 100, 0)]
    [InlineData(-50, 100, -0.5)]
    public void CalculateRecoveryRate_ShouldReturnDivision_WhenLoanAmountIsNotZero(decimal scenarioCollateralValue, decimal loanAmount, decimal expected)
    {
        var result = _sut.CalculateRecoveryRate(scenarioCollateralValue, loanAmount);

        result.ShouldBe(expected);
    }

    [Fact]
    public void CalculateRecoveryRate_ShouldReturnZero_WhenLoanAmountIsZero()
    {
        var result = _sut.CalculateRecoveryRate(100m, 0m);

        result.ShouldBe(0m);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 0)]
    [InlineData(1.2, -0.2)]
    public void CalculateLossGivenDefault_ShouldReturnOneMinusRecoveryRate(decimal recoveryRate, decimal expected)
    {
        var result = _sut.CalculateLossGivenDefault(recoveryRate);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.05, 0.4, 1000, 20)]
    [InlineData(0, 0.4, 1000, 0)]
    [InlineData(0.05, 0, 1000, 0)]
    [InlineData(0.05, 0.4, 0, 0)]
    [InlineData(-0.1, 0.5, 100, -5)]
    public void CalculateExpectedLoss_ShouldMultiplyInputs(decimal probabilityOfDefault, decimal lossGivenDefault, decimal outstandingAmount, decimal expected)
    {
        var result = _sut.CalculateExpectedLoss(probabilityOfDefault, lossGivenDefault, outstandingAmount);

        result.ShouldBe(expected);
    }
}