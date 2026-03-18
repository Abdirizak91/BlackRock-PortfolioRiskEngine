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

    internal const string SelectRiskRunsCount = """
        SELECT COUNT(1)
        FROM risk_runs;
        """;

    internal const string SelectRiskRunsPage = """
        SELECT
            id AS RiskRunId,
            run_date_utc AS RunDateUtc,
            time_taken_ms AS TimeTakenMs
        FROM risk_runs
        ORDER BY run_date_utc DESC
        LIMIT @Limit OFFSET @Offset;
        """;

    internal const string SelectCountryChangesByRunIds = """
        SELECT
            risk_run_id AS RiskRunId,
            country_code AS CountryCode,
            percentage_change AS PercentageChange
        FROM risk_run_country_changes
        WHERE risk_run_id IN @RunIds;
        """;

    internal const string SelectPortfolioResultsByRunIds = """
        SELECT
            risk_run_id AS RiskRunId,
            portfolio_id AS PortfolioId,
            portfolio_name AS PortfolioName,
            country AS Country,
            currency AS Currency,
            total_outstanding_amount AS TotalOutstandingAmount,
            total_collateral_value AS TotalCollateralValue,
            total_scenario_collateral_value AS TotalScenarioCollateralValue,
            total_expected_loss AS TotalExpectedLoss
        FROM risk_run_portfolio_results
        WHERE risk_run_id IN @RunIds;
        """;
}

