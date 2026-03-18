namespace PortfolioRiskEngine.Application.DTOs;

public record SearchRunsResponseDto
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public List<ScenarioResultDto> Runs { get; init; } = [];
}
