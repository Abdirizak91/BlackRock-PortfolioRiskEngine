namespace PortfolioRiskEngine.Application.DTOs;

public record ScenarioResultDto
{
    public DateTime RunDate { get; init; }
    public long TimeTakenMs { get; init; }
    public Dictionary<string, decimal> CountryPercentageChanges { get; init; } = new();
    public List<PortfolioRiskResultDto> PortfolioResults { get; init; } = [];
}
