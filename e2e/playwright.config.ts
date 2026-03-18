import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  retries: 0,
  use: {
    baseURL: 'http://localhost:5118',
    extraHTTPHeaders: {
      'Content-Type': 'application/json',
    },
  },
  webServer: {
    command: 'dotnet run --project ../PortfolioRiskEngine/PortfolioRiskEngine.Api',
    url: 'http://localhost:5118/swagger/index.html',
    reuseExistingServer: true,
    timeout: 30_000,
  },
})
