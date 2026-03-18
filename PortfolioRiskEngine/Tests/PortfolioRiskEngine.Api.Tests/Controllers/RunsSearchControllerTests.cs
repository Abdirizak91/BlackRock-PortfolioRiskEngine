using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortfolioRiskEngine.Api.Controllers;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;
using Shouldly;

namespace PortfolioRiskEngine.Api.Tests.Controllers;

public class RunsSearchControllerTests
{
    [Fact]
    public async Task SearchRuns_ShouldReturnOk_WhenSearchSucceeds()
    {
        var orchestrator = Substitute.For<ISearchRunsOrchestrator>();
        var logger = Substitute.For<ILogger<RunsSearchController>>();
        var sut = new RunsSearchController(orchestrator, logger);
        var request = new SearchRunsRequestDto { PageNumber = 1, PageSize = 10 };
        var response = new SearchRunsResponseDto
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1,
            Runs =
            [
                new ScenarioResultDto
                {
                    RunDate = DateTime.UtcNow,
                    TimeTakenMs = 12,
                    CountryPercentageChanges = new Dictionary<string, decimal> { ["US"] = -0.1m },
                    PortfolioResults = []
                }
            ]
        };

        orchestrator.SearchRunsAsync(request).Returns(Result<SearchRunsResponseDto>.Success(response));

        var actionResult = await sut.SearchRuns(request);

        var ok = actionResult.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SearchRunsResponseDto>();
        payload.Runs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchRuns_ShouldReturnServiceUnavailable_WhenDependencyFails()
    {
        var orchestrator = Substitute.For<ISearchRunsOrchestrator>();
        var logger = Substitute.For<ILogger<RunsSearchController>>();
        var sut = new RunsSearchController(orchestrator, logger);
        var request = new SearchRunsRequestDto { PageNumber = 1, PageSize = 10 };

        orchestrator.SearchRunsAsync(request)
            .Returns(Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.PersistenceFailed()));

        var actionResult = await sut.SearchRuns(request);

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task SearchRuns_ShouldReturnBadRequest_WhenValidationFails()
    {
        var orchestrator = Substitute.For<ISearchRunsOrchestrator>();
        var logger = Substitute.For<ILogger<RunsSearchController>>();
        var sut = new RunsSearchController(orchestrator, logger);
        var request = new SearchRunsRequestDto { PageNumber = 0, PageSize = 200 };

        orchestrator.SearchRunsAsync(request)
            .Returns(Result<SearchRunsResponseDto>.Failure(RiskEngineErrors.InvalidPagination()));

        var actionResult = await sut.SearchRuns(request);

        var badRequest = actionResult.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("PageNumber must be >= 1 and PageSize must be between 1 and 100.");
    }
}

