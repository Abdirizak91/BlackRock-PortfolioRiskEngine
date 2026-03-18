import { useEffect, useState } from 'react'
import { RiskEngineClient } from '../clients/RiskEngineClient'
import type { ScenarioResult, SearchRunsResponse } from '../models/SearchRunsResponse'
import './RunHistoryPage.css'

const riskEngineClient = new RiskEngineClient()
const PAGE_SIZE = 10

function RunHistoryPage() {
  const [data, setData] = useState<SearchRunsResponse | null>(null)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [expandedRun, setExpandedRun] = useState<number | null>(null)

  const fetchRuns = async (pageNumber: number) => {
    setLoading(true)
    setError(null)
    try {
      const result = await riskEngineClient.searchRuns(pageNumber, PAGE_SIZE)
      setData(result)
      setExpandedRun(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchRuns(page)
  }, [page])

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0

  const formatDate = (iso: string) => {
    const d = new Date(iso)
    return d.toLocaleString()
  }

  const formatNumber = (n: number) =>
    n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })

  const toggleExpand = (index: number) => {
    setExpandedRun(prev => (prev === index ? null : index))
  }

  return (
    <div className="history-page">
      <header className="app-header">
        <h1>Run History</h1>
        <p>View previous risk calculation runs</p>
      </header>

      <main>
        {error && <div className="error-message">{error}</div>}

        {loading && <div className="loading">Loading…</div>}

        {!loading && data && data.runs.length === 0 && (
          <div className="empty-state">No runs found.</div>
        )}

        {!loading && data && data.runs.length > 0 && (
          <>
            <div className="runs-list">
              {data.runs.map((run: ScenarioResult, index: number) => (
                <div key={index} className="run-card">
                  <button
                    type="button"
                    className="run-card-header"
                    onClick={() => toggleExpand(index)}
                    aria-expanded={expandedRun === index}
                  >
                    <div className="run-meta">
                      <span className="run-date">{formatDate(run.runDate)}</span>
                      <span className="run-time">{run.timeTakenMs}ms</span>
                    </div>
                    <div className="run-scenarios">
                      {Object.entries(run.countryPercentageChanges).map(([code, pct]) => (
                        <span key={code} className="scenario-badge">
                          {code} {pct > 0 ? '+' : ''}{pct}%
                        </span>
                      ))}
                    </div>
                    <span className="expand-icon">{expandedRun === index ? '▲' : '▼'}</span>
                  </button>

                  {expandedRun === index && (
                    <div className="run-card-body">
                      <table className="results-table">
                        <thead>
                          <tr>
                            <th>Portfolio</th>
                            <th>Country</th>
                            <th>Currency</th>
                            <th>Outstanding</th>
                            <th>Collateral</th>
                            <th>Scenario CV</th>
                            <th>Expected Loss</th>
                          </tr>
                        </thead>
                        <tbody>
                          {run.portfolioResults.map(pr => (
                            <tr key={pr.portfolioId}>
                              <td className="cell-name">{pr.portfolioName}</td>
                              <td>{pr.country}</td>
                              <td>{pr.currency}</td>
                              <td className="cell-number">{formatNumber(pr.totalOutstandingAmount)}</td>
                              <td className="cell-number">{formatNumber(pr.totalCollateralValue)}</td>
                              <td className="cell-number">{formatNumber(pr.totalScenarioCollateralValue)}</td>
                              <td className="cell-number cell-loss">{formatNumber(pr.totalExpectedLoss)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="pagination">
              <button
                className="btn btn-secondary"
                disabled={page <= 1}
                onClick={() => setPage(p => p - 1)}
              >
                ← Previous
              </button>
              <span className="page-info">
                Page {data.pageNumber} of {totalPages} ({data.totalCount} total)
              </span>
              <button
                className="btn btn-secondary"
                disabled={page >= totalPages}
                onClick={() => setPage(p => p + 1)}
              >
                Next →
              </button>
            </div>
          </>
        )}
      </main>
    </div>
  )
}

export default RunHistoryPage
