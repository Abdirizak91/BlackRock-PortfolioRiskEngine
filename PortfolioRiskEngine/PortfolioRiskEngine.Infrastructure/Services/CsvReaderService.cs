using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Domain.Entities;

namespace PortfolioRiskEngine.Infrastructure.Services;

public class CsvReaderService(string dataDirectory) : ICsvReaderService
{
    public async Task<List<Loan>> ReadLoansAsync()
    {
        var filePath = Path.Combine(dataDirectory, "loans.csv");
        var lines = await File.ReadAllLinesAsync(filePath);

        return lines.Skip(1) // skip header
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var parts = line.Split(',');
                return new Loan
                {
                    LoanId = int.Parse(parts[0].Trim()),
                    PortId = int.Parse(parts[1].Trim()),
                    OriginalLoanAmount = decimal.Parse(parts[2].Trim()),
                    OutstandingAmount = decimal.Parse(parts[3].Trim()),
                    CollateralValue = decimal.Parse(parts[4].Trim()),
                    CreditRating = parts[5].Trim()
                };
            })
            .ToList();
    }

    public async Task<List<Portfolio>> ReadPortfoliosAsync()
    {
        var filePath = Path.Combine(dataDirectory, "portfolios.csv");
        var lines = await File.ReadAllLinesAsync(filePath);

        return lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var parts = line.Split(',');
                return new Portfolio
                {
                    PortId = int.Parse(parts[0].Trim()),
                    PortName = parts[1].Trim(),
                    PortCountry = parts[2].Trim(),
                    PortCcy = parts[3].Trim()
                };
            })
            .ToList();
    }

    public async Task<List<Rating>> ReadRatingsAsync()
    {
        var filePath = Path.Combine(dataDirectory, "ratings.csv");
        var lines = await File.ReadAllLinesAsync(filePath);

        return lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var parts = line.Split(',');
                return new Rating
                {
                    RatingCode = parts[0].Trim(),
                    ProbabilityOfDefault = decimal.Parse(parts[1].Trim())
                };
            })
            .ToList();
    }
}
