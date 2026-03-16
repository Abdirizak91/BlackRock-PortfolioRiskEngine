using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Domain.Entities;
using PortfolioRiskEngine.Domain.Services;

namespace PortfolioRiskEngine.Application.Orchestrators;

public interface IRiskEngineOrchestrator
{
    Task<ScenarioResultDto> CalculateRiskAsync(ScenarioRequestDto request);
}

public class RiskEngineOrchestrator(
    ICsvReaderService csvReaderService,
    IRiskCalculator riskCalculator,
    ILogger<RiskEngineOrchestrator> logger)
    : IRiskEngineOrchestrator
{
    public async Task<ScenarioResultDto> CalculateRiskAsync(ScenarioRequestDto request)
    {
        logger.LogInformation("Starting risk calculation with {Count} country inputs", request.CountryPercentageChanges.Count);

        var stopwatch = Stopwatch.StartNew();
        var (portfolios, loansByPortfolio, ratingLookup) = await LoadRiskInputsAsync();
        var portfolioResults = CalculatePortfolioResults(portfolios, loansByPortfolio, ratingLookup, request.CountryPercentageChanges);

        stopwatch.Stop();
        logger.LogInformation("Risk calculation completed in {Ms}ms", stopwatch.ElapsedMilliseconds);

        return BuildScenarioResult(request, portfolioResults, stopwatch.ElapsedMilliseconds);
    }

    private async Task<(IReadOnlyList<Portfolio> Portfolios, Dictionary<int, List<Loan>> LoansByPortfolio, Dictionary<string, decimal> RatingLookup)> LoadRiskInputsAsync()
    {
        var portfolios = await csvReaderService.ReadPortfoliosAsync();
        var loans = await csvReaderService.ReadLoansAsync();
        var ratings = await csvReaderService.ReadRatingsAsync();

        var loansByPortfolio = loans
            .GroupBy(loan => loan.PortId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var ratingLookup = ratings.ToDictionary(r => r.RatingCode, r => r.ProbabilityOfDefault);
        return (portfolios, loansByPortfolio, ratingLookup);
    }

    private List<PortfolioRiskResultDto> CalculatePortfolioResults(
        IEnumerable<Portfolio> portfolios,
        IReadOnlyDictionary<int, List<Loan>> loansByPortfolio,
        IReadOnlyDictionary<string, decimal> ratingLookup,
        IReadOnlyDictionary<string, decimal> countryPercentageChanges)
    {
        var portfolioResults = new List<PortfolioRiskResultDto>();

        foreach (var portfolio in portfolios)
        {
            if (!countryPercentageChanges.TryGetValue(portfolio.PortCountry, out var percentageChange))
                percentageChange = 0m;

            loansByPortfolio.TryGetValue(portfolio.PortId, out var portfolioLoans);
            portfolioLoans ??= new List<Loan>();

            var totals = CalculateTotals(portfolioLoans, percentageChange, ratingLookup);
            portfolioResults.Add(BuildPortfolioResult(portfolio, totals));
        }

        return portfolioResults;
    }

    private PortfolioTotals CalculateTotals(
        IEnumerable<Loan> loans,
        decimal percentageChange,
        IReadOnlyDictionary<string, decimal> ratingLookup)
    {
        var totals = new PortfolioTotals();

        foreach (var loan in loans)
        {
            var scenarioCollateral = riskCalculator.CalculateScenarioCollateralValue(loan.CollateralValue, percentageChange);
            var recoveryRate = riskCalculator.CalculateRecoveryRate(scenarioCollateral, loan.OutstandingAmount);
            var lossGivenDefault = riskCalculator.CalculateLossGivenDefault(recoveryRate);
            var probabilityOfDefault = ratingLookup.GetValueOrDefault(loan.CreditRating, 0m);
            var expectedLoss = riskCalculator.CalculateExpectedLoss(probabilityOfDefault, lossGivenDefault, loan.OutstandingAmount);

            totals.TotalOutstanding += loan.OutstandingAmount;
            totals.TotalCollateral += loan.CollateralValue;
            totals.TotalScenarioCollateral += scenarioCollateral;
            totals.TotalExpectedLoss += expectedLoss;
        }

        return totals;
    }

    private static PortfolioRiskResultDto BuildPortfolioResult(Portfolio portfolio, PortfolioTotals totals)
    {
        return new PortfolioRiskResultDto
        {
            PortfolioId = portfolio.PortId,
            PortfolioName = portfolio.PortName,
            Country = portfolio.PortCountry,
            Currency = portfolio.PortCcy,
            TotalOutstandingAmount = totals.TotalOutstanding,
            TotalCollateralValue = totals.TotalCollateral,
            TotalScenarioCollateralValue = totals.TotalScenarioCollateral,
            TotalExpectedLoss = totals.TotalExpectedLoss
        };
    }

    private static ScenarioResultDto BuildScenarioResult(
        ScenarioRequestDto request,
        List<PortfolioRiskResultDto> portfolioResults,
        long elapsedMilliseconds)
    {
        return new ScenarioResultDto
        {
            RunDate = DateTime.UtcNow,
            TimeTakenMs = elapsedMilliseconds,
            CountryPercentageChanges = request.CountryPercentageChanges,
            PortfolioResults = portfolioResults
        };
    }
}