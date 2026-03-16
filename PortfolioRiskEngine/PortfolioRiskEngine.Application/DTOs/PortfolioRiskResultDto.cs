namespace PortfolioRiskEngine.Application.DTOs;

public record PortfolioRiskResultDto
{
    public int PortfolioId { get; init; }
    public string PortfolioName { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal TotalOutstandingAmount { get; init; }
    public decimal TotalCollateralValue { get; init; }
    public decimal TotalScenarioCollateralValue { get; init; }
    public decimal TotalExpectedLoss { get; init; }
}
