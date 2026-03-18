using Microsoft.Extensions.Logging;
using NSubstitute;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Interfaces;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;
using Shouldly;

namespace PortfolioRiskEngine.Application.Tests.Orchestrators;

public class SearchRunsOrchestratorTests
{
    [Fact]
    public async Task SearchRunsAsync_ShouldReturnRuns_WhenRepositorySucceeds()
    {
        var repository = Substitute.For<IRiskResultRepository>();
        var logger = Substitute.For<ILogger<SearchRunsOrchestrator>>();
        var sut = new SearchRunsOrchestrator(repository, logger);
        var request = new SearchRunsRequestDto { PageNumber = 1, PageSize = 10 };
        var repositoryResponse = new SearchRunsResponseDto
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1,
            Runs =
            [
                new ScenarioResultDto
                {
                    RunDate = DateTime.UtcNow,
                    TimeTakenMs = 11,
                    CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m },
                    PortfolioResults = []
                }
            ]
        };

        repository.SearchScenarioResultsAsync(request).Returns(Result<SearchRunsResponseDto>.Success(repositoryResponse));

        var result = await sut.SearchRunsAsync(request);

        result.IsSuccess.ShouldBeTrue();
        var response = result.Value!;
        response.Runs.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
        response.Runs[0].TimeTakenMs.ShouldBe(11);
    }

    [Fact]
    public async Task SearchRunsAsync_ShouldReturnFailure_WhenRepositoryFails()
    {
        var repository = Substitute.For<IRiskResultRepository>();
        var logger = Substitute.For<ILogger<SearchRunsOrchestrator>>();
        var sut = new SearchRunsOrchestrator(repository, logger);

        var request = new SearchRunsRequestDto { PageNumber = 1, PageSize = 10 };

        repository.SearchScenarioResultsAsync(Arg.Any<SearchRunsRequestDto>())
            .Returns(Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.PersistenceFailed()));

        var result = await sut.SearchRunsAsync(request);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchRunsAsync_ShouldReturnValidationFailure_WhenPaginationIsInvalid()
    {
        var repository = Substitute.For<IRiskResultRepository>();
        var logger = Substitute.For<ILogger<SearchRunsOrchestrator>>();
        var sut = new SearchRunsOrchestrator(repository, logger);
        var request = new SearchRunsRequestDto { PageNumber = 0, PageSize = 200 };

        var result = await sut.SearchRunsAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("risk.validation.invalid_pagination");
        await repository.DidNotReceive().SearchScenarioResultsAsync(Arg.Any<SearchRunsRequestDto>());
    }
}
