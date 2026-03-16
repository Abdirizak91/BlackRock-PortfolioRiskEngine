import { useState } from 'react'
import { DEFAULT_SCENARIOS, COUNTRY_NAMES } from '../models/CountryConfig'
import type { ScenarioRequest } from '../models/ScenarioRequest'
import './HomePage.css'

const API_URL = 'http://localhost:5118/riskengine/calculaterisk'

function HomePage() {
  const [percentages, setPercentages] = useState<Record<string, string>>(
    Object.fromEntries(
      Object.entries(DEFAULT_SCENARIOS).map(([code, val]) => [code, val.toString()])
    )
  )
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleChange = (country: string, value: string) => {
    setPercentages(prev => ({ ...prev, [country]: value }))
    setError(null)
  }

  const handleReset = () => {
    setPercentages(
      Object.fromEntries(
        Object.entries(DEFAULT_SCENARIOS).map(([code, val]) => [code, val.toString()])
      )
    )
    setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    const parsed: Record<string, number> = {}
    for (const [country, val] of Object.entries(percentages)) {
      const num = parseFloat(val)
      if (isNaN(num)) {
        setError(`Invalid value for ${country}: "${val}"`)
        return
      }
      parsed[country] = num
    }

    const allZero = Object.values(parsed).every(v => v === 0)
    if (allZero) {
      setError('You must provide a non-zero percentage value for at least one country.')
      return
    }

    const request: ScenarioRequest = {
      countryPercentageChanges: parsed,
    }

    setLoading(true)
    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })

      if (!response.ok) {
        const text = await response.text()
        setError(`Server error (${response.status}): ${text}`)
        return
      }

      const data = await response.json()
      console.log('Scenario result:', data)
    } catch (err) {
      setError(`Failed to connect to server: ${err instanceof Error ? err.message : String(err)}`)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="home-page">
      <header className="app-header">
        <h1>Portfolio Risk Engine</h1>
        <p>Specify percentage change in house prices by country</p>
      </header>

      <main>
        <form onSubmit={handleSubmit} className="scenario-form">
          <table className="scenario-table">
            <thead>
              <tr>
                <th>Country</th>
                <th>Code</th>
                <th>% Change</th>
              </tr>
            </thead>
            <tbody>
              {Object.keys(DEFAULT_SCENARIOS).map(code => (
                <tr key={code}>
                  <td className="country-name">{COUNTRY_NAMES[code]}</td>
                  <td className="country-code">{code}</td>
                  <td>
                    <input
                      type="number"
                      step="0.01"
                      value={percentages[code]}
                      onChange={e => handleChange(code, e.target.value)}
                      className="pct-input"
                      aria-label={`Percentage change for ${COUNTRY_NAMES[code]}`}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {error && <div className="error-message">{error}</div>}

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Running…' : 'Run Scenario'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={handleReset} disabled={loading}>
              Reset Defaults
            </button>
          </div>
        </form>
      </main>
    </div>
  )
}

export default HomePage
