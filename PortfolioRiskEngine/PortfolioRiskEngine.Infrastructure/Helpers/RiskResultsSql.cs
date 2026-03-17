namespace PortfolioRiskEngine.Infrastructure.Helpers;

internal static class RiskResultsSql
{
    internal const string CreateRiskRunsTable = """
        CREATE TABLE IF NOT EXISTS risk_runs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            run_date_utc TEXT NOT NULL,
            time_taken_ms INTEGER NOT NULL,
            created_utc TEXT NOT NULL
        );
        """;

    internal const string CreateCountryChangesTable = """
        CREATE TABLE IF NOT EXISTS risk_run_country_changes (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            risk_run_id INTEGER NOT NULL,
            country_code TEXT NOT NULL,
            percentage_change REAL NOT NULL,
            FOREIGN KEY(risk_run_id) REFERENCES risk_runs(id)
        );
        """;

    internal const string CreatePortfolioResultsTable = """
        CREATE TABLE IF NOT EXISTS risk_run_portfolio_results (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            risk_run_id INTEGER NOT NULL,
            portfolio_id INTEGER NOT NULL,
            portfolio_name TEXT NOT NULL,
            country TEXT NOT NULL,
            currency TEXT NOT NULL,
            total_outstanding_amount REAL NOT NULL,
            total_collateral_value REAL NOT NULL,
            total_scenario_collateral_value REAL NOT NULL,
            total_expected_loss REAL NOT NULL,
            FOREIGN KEY(risk_run_id) REFERENCES risk_runs(id)
        );
        """;

    internal const string InsertRiskRun = """
        INSERT INTO risk_runs (run_date_utc, time_taken_ms, created_utc)
        VALUES (@RunDateUtc, @TimeTakenMs, @CreatedUtc);
        SELECT last_insert_rowid();
        """;

    internal const string InsertCountryChange = """
        INSERT INTO risk_run_country_changes (risk_run_id, country_code, percentage_change)
        VALUES (@RiskRunId, @CountryCode, @PercentageChange);
        """;

    internal const string InsertPortfolioResult = """
        INSERT INTO risk_run_portfolio_results (
            risk_run_id,
            portfolio_id,
            portfolio_name,
            country,
            currency,
            total_outstanding_amount,
            total_collateral_value,
            total_scenario_collateral_value,
            total_expected_loss
        )
        VALUES (
            @RiskRunId,
            @PortfolioId,
            @PortfolioName,
            @Country,
            @Currency,
            @TotalOutstandingAmount,
            @TotalCollateralValue,
            @TotalScenarioCollateralValue,
            @TotalExpectedLoss
        );
        """;
}

