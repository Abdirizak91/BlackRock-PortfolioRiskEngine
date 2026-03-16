namespace PortfolioRiskEngine.Domain.Services;

public interface IRiskCalculator
{
    decimal CalculateScenarioCollateralValue(decimal collateralValue, decimal percentageChange);
    decimal CalculateRecoveryRate(decimal scenarioCollateralValue, decimal loanAmount);
    decimal CalculateLossGivenDefault(decimal recoveryRate);
    decimal CalculateExpectedLoss(decimal probabilityOfDefault, decimal lossGivenDefault, decimal outstandingAmount);
}

public class RiskCalculator : IRiskCalculator
{
    public decimal CalculateScenarioCollateralValue(decimal collateralValue, decimal percentageChange)
    {
        return collateralValue * percentageChange;
    }

    public decimal CalculateRecoveryRate(decimal scenarioCollateralValue, decimal loanAmount)
    {
        if (loanAmount == 0)
            return 0;

        return scenarioCollateralValue / loanAmount;
    }

    public decimal CalculateLossGivenDefault(decimal recoveryRate)
    {
        return 1 - recoveryRate;
    }

    public decimal CalculateExpectedLoss(decimal probabilityOfDefault, decimal lossGivenDefault, decimal outstandingAmount)
    {
        return probabilityOfDefault * lossGivenDefault * outstandingAmount;
    }
}