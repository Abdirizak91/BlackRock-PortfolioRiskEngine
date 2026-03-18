import type { ScenarioRequest } from '../models/ScenarioRequest'
import type { SearchRunsResponse } from '../models/SearchRunsResponse'

const API_BASE_URL = 'http://localhost:5118'

export class RiskEngineClient {
  async calculateRisk(request: ScenarioRequest): Promise<number> {
    const response = await fetch(`${API_BASE_URL}/riskengine/calculate-risk`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const text = await response.text()
      throw new Error(`Server error (${response.status}): ${text}`)
    }

    return response.status
  }

  async searchRuns(pageNumber: number, pageSize: number): Promise<SearchRunsResponse> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    })

    const response = await fetch(`${API_BASE_URL}/RunsSearch/search-runs?${params}`)

    if (!response.ok) {
      const text = await response.text()
      throw new Error(`Server error (${response.status}): ${text}`)
    }

    return await response.json() as SearchRunsResponse
  }
}
