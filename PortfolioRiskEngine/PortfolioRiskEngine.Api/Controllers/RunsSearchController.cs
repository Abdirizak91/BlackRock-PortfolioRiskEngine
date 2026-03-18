using Microsoft.AspNetCore.Mvc;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;

namespace PortfolioRiskEngine.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RunsSearchController(ISearchRunsOrchestrator searchRunsOrchestrator, ILogger<RunsSearchController> logger) : ControllerBase
{
    [HttpGet("search-runs")]
    [ProducesResponseType(typeof(SearchRunsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchRuns([FromQuery] SearchRunsRequestDto request)
    {
        var result = await searchRunsOrchestrator.SearchRunsAsync(request);

        if (result.IsFailure)
        {
            var primaryError = result.Errors[0];

            return primaryError switch
            {
                ValidationError => BadRequest(primaryError.Message),
                DependencyError => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: primaryError.Message),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: primaryError.Message)
            };
        }

        var response = result.Value!;
        logger.LogInformation("Search-runs returned {Count} saved runs.", response.Runs.Count);
        return Ok(response);
    }
}