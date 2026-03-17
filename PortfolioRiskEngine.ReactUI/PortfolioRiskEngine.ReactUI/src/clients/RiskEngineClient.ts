import type { ScenarioRequest } from '../models/ScenarioRequest'

const API_BASE_URL = 'http://localhost:5118'

export class RiskEngineClient {
  async calculateRisk(request: ScenarioRequest): Promise<unknown> {
    const response = await fetch(`${API_BASE_URL}/riskengine/calculate-risk`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const text = await response.text()
      throw new Error(`Server error (${response.status}): ${text}`)
    }

    return await response.json()
  }
}
