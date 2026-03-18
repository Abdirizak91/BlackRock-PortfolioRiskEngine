namespace PortfolioRiskEngine.Application.DTOs;

public record SearchRunsRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
