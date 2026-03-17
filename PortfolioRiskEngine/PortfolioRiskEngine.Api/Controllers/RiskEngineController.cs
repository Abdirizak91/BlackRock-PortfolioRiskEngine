using Microsoft.AspNetCore.Mvc;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;
using PortfolioRiskEngine.Application.Results;

namespace PortfolioRiskEngine.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RiskEngineController(IRiskEngineOrchestrator riskEngineOrchestrator, ILogger<RiskEngineController> logger) : ControllerBase
{
    [HttpPost("calculate-risk")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateRisk([FromBody] ScenarioRequestDto request)
    {
        logger.LogInformation("CalculateRisk endpoint called with {Count} countries", request.CountryPercentageChanges.Count);

        var result = await riskEngineOrchestrator.CalculateRiskAsync(request);

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

        logger.LogInformation("Risk calculation completed successfully.");
        
        return Created();
    }
}