namespace PortfolioRiskEngine.Application.Results;

public static class RiskEngineErrors
{
    public static ValidationError InvalidPagination() =>
        new("risk.validation.invalid_pagination", "PageNumber must be >= 1 and PageSize must be between 1 and 100.");

    public static ValidationError InvalidCountryChanges() =>
        new("risk.validation.country_changes_required", "At least one country percentage change is required.");

    public static DependencyError InputDataUnavailable() =>
        new("risk.dependency.input_data_unavailable", "Unable to load risk input data.");

    public static DependencyError PersistenceFailed() =>
        new("risk.dependency.persistence_failed", "Risk result could not be persisted.");

    public static DependencyError PersistenceConfigurationMissing() =>
        new("risk.dependency.persistence_config_missing", "Risk persistence connection string is missing.");

    public static UnexpectedError UnexpectedFailure() =>
        new("risk.unexpected.failure", "An unexpected error occurred while calculating risk.");
}
