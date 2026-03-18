using Microsoft.Extensions.Logging;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Results;

namespace PortfolioRiskEngine.Application.Orchestrators;

public interface ISearchRunsOrchestrator
{
    Task<Result<SearchRunsResponseDto>> SearchRunsAsync(SearchRunsRequestDto request);
}

public class SearchRunsOrchestrator(
    IRiskResultRepository riskResultRepository,
    ILogger<SearchRunsOrchestrator> logger) : ISearchRunsOrchestrator
{
    public async Task<Result<SearchRunsResponseDto>> SearchRunsAsync(SearchRunsRequestDto request)
    {
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100)
            return Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.InvalidPagination());

        var result = await riskResultRepository.SearchScenarioResultsAsync(request);

        if (result.IsFailure)
        {
            logger.LogInformation("Search-runs failed with error: {ErrorMessage}", result.Errors[0].Message);
            return Result<SearchRunsResponseDto>.Failure(result.Errors);
        }

        return Result<SearchRunsResponseDto>.Success(result.Value!);
    }
}