namespace PortfolioRiskEngine.Infrastructure.Services;

public sealed class RiskResultsDatabaseOptions
{
    public const string SectionName = "RiskResultsDatabase";

    public string ConnectionString { get; set; } = string.Empty;
}

