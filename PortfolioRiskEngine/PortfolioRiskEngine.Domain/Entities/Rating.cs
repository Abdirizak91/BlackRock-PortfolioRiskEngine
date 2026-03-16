namespace PortfolioRiskEngine.Domain.Entities;

public record Rating
{
    public string RatingCode { get; init; } = string.Empty;
    public decimal ProbabilityOfDefault { get; init; }
}
