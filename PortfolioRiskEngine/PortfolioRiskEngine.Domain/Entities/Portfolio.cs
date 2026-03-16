namespace PortfolioRiskEngine.Domain.Entities;

public record Portfolio
{
    public int PortId { get; init; }
    public string PortName { get; init; } = string.Empty;
    public string PortCountry { get; init; } = string.Empty;
    public string PortCcy { get; init; } = string.Empty;
}
