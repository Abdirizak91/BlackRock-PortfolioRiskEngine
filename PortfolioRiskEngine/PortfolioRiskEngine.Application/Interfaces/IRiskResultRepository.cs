using PortfolioRiskEngine.Application.DTOs;

namespace PortfolioRiskEngine.Application.Interfaces;

public interface IRiskResultRepository
{
    Task SaveScenarioResultAsync(ScenarioResultDto result);
}