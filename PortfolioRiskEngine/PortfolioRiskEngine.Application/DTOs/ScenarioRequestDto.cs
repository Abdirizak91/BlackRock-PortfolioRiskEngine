namespace PortfolioRiskEngine.Application.DTOs;

public record ScenarioRequestDto
{
    public required Dictionary<string, decimal> CountryPercentageChanges { get; init; }
}
