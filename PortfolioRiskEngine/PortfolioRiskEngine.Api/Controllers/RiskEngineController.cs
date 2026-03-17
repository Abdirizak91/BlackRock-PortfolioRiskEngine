using Microsoft.AspNetCore.Mvc;
using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Orchestrators;

namespace PortfolioRiskEngine.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RiskEngineController(IRiskEngineOrchestrator riskEngineOrchestrator, ILogger<RiskEngineController> logger) : ControllerBase
{
    [HttpPost("calculate-risk")]
    [ProducesResponseType(typeof(ScenarioResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateRisk([FromBody] ScenarioRequestDto request)
    {
        if (request.CountryPercentageChanges.Count == 0)
            return BadRequest("At least one country percentage change is required.");

        logger.LogInformation("CalculateRisk endpoint called with {Count} countries", request.CountryPercentageChanges.Count);

        var result = await riskEngineOrchestrator.CalculateRiskAsync(request);

        logger.LogInformation("Risk calculation completed in {Ms}ms", result.TimeTakenMs);
        return Ok(result);
    }
}