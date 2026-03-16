namespace PortfolioRiskEngine.Application.Orchestrators;

internal sealed class PortfolioTotals
{
    public decimal TotalOutstanding { get; set; }
    public decimal TotalCollateral { get; set; }
    public decimal TotalScenarioCollateral { get; set; }
    public decimal TotalExpectedLoss { get; set; }
}

