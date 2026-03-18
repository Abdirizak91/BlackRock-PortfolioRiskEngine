export interface PortfolioRiskResult {
  portfolioId: number
  portfolioName: string
  country: string
  currency: string
  totalOutstandingAmount: number
  totalCollateralValue: number
  totalScenarioCollateralValue: number
  totalExpectedLoss: number
}

export interface ScenarioResult {
  runDate: string
  timeTakenMs: number
  countryPercentageChanges: Record<string, number>
  portfolioResults: PortfolioRiskResult[]
}

export interface SearchRunsResponse {
  pageNumber: number
  pageSize: number
  totalCount: number
  runs: ScenarioResult[]
}
