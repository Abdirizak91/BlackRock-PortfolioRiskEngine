using PortfolioRiskEngine.Domain.Entities;

namespace PortfolioRiskEngine.Application.Interfaces;

public interface ICsvReaderService
{
    Task<List<Loan>> ReadLoansAsync();
    Task<List<Portfolio>> ReadPortfoliosAsync();
    Task<List<Rating>> ReadRatingsAsync();
}
