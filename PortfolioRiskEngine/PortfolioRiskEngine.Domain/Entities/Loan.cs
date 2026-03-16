namespace PortfolioRiskEngine.Domain.Entities;

public record Loan
{
    public int LoanId { get; init; }
    public int PortId { get; init; }
    public decimal OriginalLoanAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public decimal CollateralValue { get; init; }
    public string CreditRating { get; init; } = string.Empty;
}
