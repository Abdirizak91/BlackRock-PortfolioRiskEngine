using PortfolioRiskEngine.Application.DTOs;
using PortfolioRiskEngine.Application.Results;

namespace PortfolioRiskEngine.Application.Interfaces;

public interface IRiskResultRepository
{
    Task<Result> SaveScenarioResultAsync(ScenarioResultDto result);
}