import '@testing-library/jest-dom'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import RunHistoryPage from '../RunHistoryPage'
import { RiskEngineClient } from '../../clients/RiskEngineClient'
import type { SearchRunsResponse } from '../../models/SearchRunsResponse'

jest.mock('../../clients/RiskEngineClient')

const MockedRiskEngineClient = RiskEngineClient as jest.MockedClass<typeof RiskEngineClient>

const buildResponse = (overrides: Partial<SearchRunsResponse> = {}): SearchRunsResponse => ({
  pageNumber: 1,
  pageSize: 10,
  totalCount: 2,
  runs: [
    {
      runDate: '2026-03-18T10:00:00Z',
      timeTakenMs: 123,
      countryPercentageChanges: { US: -4.34, GB: -5.12 },
      portfolioResults: [
        {
          portfolioId: 1,
          portfolioName: 'Portfolio A',
          country: 'US',
          currency: 'USD',
          totalOutstandingAmount: 1000000,
          totalCollateralValue: 800000,
          totalScenarioCollateralValue: 750000,
          totalExpectedLoss: 50000,
        },
      ],
    },
    {
      runDate: '2026-03-17T09:00:00Z',
      timeTakenMs: 98,
      countryPercentageChanges: { FR: -3.87 },
      portfolioResults: [
        {
          portfolioId: 2,
          portfolioName: 'Portfolio B',
          country: 'FR',
          currency: 'EUR',
          totalOutstandingAmount: 500000,
          totalCollateralValue: 400000,
          totalScenarioCollateralValue: 380000,
          totalExpectedLoss: 20000,
        },
      ],
    },
  ],
  ...overrides,
})

beforeEach(() => {
  jest.clearAllMocks()
})

describe('RunHistoryPage', () => {
  it('renders the header', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(buildResponse())

    render(<RunHistoryPage />)

    expect(screen.getByText('Run History')).toBeInTheDocument()
    expect(screen.getByText('View previous risk calculation runs')).toBeInTheDocument()
  })

  it('shows loading state while fetching', async () => {
    let resolvePromise!: (value: SearchRunsResponse) => void
    MockedRiskEngineClient.prototype.searchRuns.mockImplementation(
      () => new Promise(resolve => { resolvePromise = resolve })
    )

    render(<RunHistoryPage />)

    expect(screen.getByText('Loading…')).toBeInTheDocument()

    resolvePromise(buildResponse())
    await waitFor(() => {
      expect(screen.queryByText('Loading…')).not.toBeInTheDocument()
    })
  })

  it('displays empty state when no runs exist', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(
      buildResponse({ runs: [], totalCount: 0 })
    )

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText('No runs found.')).toBeInTheDocument()
    })
  })

  it('renders run cards with scenario badges', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(buildResponse())

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/123ms/)).toBeInTheDocument()
    })

    expect(screen.getByText('US -4.34%')).toBeInTheDocument()
    expect(screen.getByText('GB -5.12%')).toBeInTheDocument()
    expect(screen.getByText('FR -3.87%')).toBeInTheDocument()
  })

  it('expands a run card to show portfolio results on click', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(buildResponse())
    const user = userEvent.setup()

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/123ms/)).toBeInTheDocument()
    })

    // Portfolio details should not be visible initially
    expect(screen.queryByText('Portfolio A')).not.toBeInTheDocument()

    // Click the first run card header to expand
    const expandButtons = screen.getAllByRole('button', { expanded: false })
    await user.click(expandButtons[0])

    expect(screen.getByText('Portfolio A')).toBeInTheDocument()
    expect(screen.getByText('US')).toBeInTheDocument()
    expect(screen.getByText('USD')).toBeInTheDocument()
  })

  it('collapses a run card when clicked again', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(buildResponse())
    const user = userEvent.setup()

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/123ms/)).toBeInTheDocument()
    })

    const expandButtons = screen.getAllByRole('button', { expanded: false })
    await user.click(expandButtons[0])

    expect(screen.getByText('Portfolio A')).toBeInTheDocument()

    // Click again to collapse
    const collapseButton = screen.getByRole('button', { expanded: true })
    await user.click(collapseButton)

    expect(screen.queryByText('Portfolio A')).not.toBeInTheDocument()
  })

  it('shows error message when fetch fails', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockRejectedValue(
      new Error('Server error (503): Service Unavailable')
    )

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/server error \(503\)/i)).toBeInTheDocument()
    })
  })

  it('shows pagination controls and total count', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(
      buildResponse({ totalCount: 25, pageNumber: 1 })
    )

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument()
      expect(screen.getByText(/25 total/i)).toBeInTheDocument()
    })

    expect(screen.getByRole('button', { name: /previous/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /next/i })).toBeEnabled()
  })

  it('navigates to next page when Next is clicked', async () => {
    MockedRiskEngineClient.prototype.searchRuns
      .mockResolvedValueOnce(buildResponse({ totalCount: 25, pageNumber: 1 }))
      .mockResolvedValueOnce(buildResponse({ totalCount: 25, pageNumber: 2 }))
    const user = userEvent.setup()

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /next/i }))

    await waitFor(() => {
      expect(MockedRiskEngineClient.prototype.searchRuns).toHaveBeenCalledWith(2, 10)
    })
  })

  it('disables Previous button on first page', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(
      buildResponse({ totalCount: 25, pageNumber: 1 })
    )

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /previous/i })).toBeDisabled()
    })
  })

  it('disables Next button on last page', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(
      buildResponse({ totalCount: 2, pageNumber: 1 })
    )

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /next/i })).toBeDisabled()
    })
  })

  it('calls searchRuns with page 1 and page size 10 on initial load', async () => {
    MockedRiskEngineClient.prototype.searchRuns.mockResolvedValue(buildResponse())

    render(<RunHistoryPage />)

    await waitFor(() => {
      expect(MockedRiskEngineClient.prototype.searchRuns).toHaveBeenCalledWith(1, 10)
    })
  })
})
