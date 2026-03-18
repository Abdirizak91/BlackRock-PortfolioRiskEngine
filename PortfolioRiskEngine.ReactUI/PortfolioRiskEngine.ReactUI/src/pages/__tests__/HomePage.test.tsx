import '@testing-library/jest-dom'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import HomePage from '../HomePage'
import { RiskEngineClient } from '../../clients/RiskEngineClient'
import { DEFAULT_SCENARIOS, COUNTRY_NAMES } from '../../models/CountryConfig'

jest.mock('../../clients/RiskEngineClient')

const MockedRiskEngineClient = RiskEngineClient as jest.MockedClass<typeof RiskEngineClient>

beforeEach(() => {
  jest.clearAllMocks()
})

describe('HomePage', () => {
  it('renders the header and form', () => {
    render(<HomePage />)

    expect(screen.getByText('Portfolio Risk Engine')).toBeInTheDocument()
    expect(screen.getByText('Specify percentage change in house prices by country')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /run scenario/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /reset defaults/i })).toBeInTheDocument()
  })

  it('renders a row for each country with default values', () => {
    render(<HomePage />)

    for (const [code, defaultVal] of Object.entries(DEFAULT_SCENARIOS)) {
      expect(screen.getByText(COUNTRY_NAMES[code])).toBeInTheDocument()
      expect(screen.getByText(code)).toBeInTheDocument()

      const input = screen.getByLabelText(`Percentage change for ${COUNTRY_NAMES[code]}`) as HTMLInputElement
      expect(input.value).toBe(defaultVal.toString())
    }
  })

  it('shows success message when API returns 201', async () => {
    MockedRiskEngineClient.prototype.calculateRisk.mockResolvedValue(201)
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    await waitFor(() => {
      expect(screen.getByText(/risk calculations were successful/i)).toBeInTheDocument()
    })

    expect(screen.getByText(/risk calculations were successful/i).closest('div'))
      .toHaveClass('success-message')
  })

  it('shows error message when API returns unexpected status', async () => {
    MockedRiskEngineClient.prototype.calculateRisk.mockResolvedValue(200)
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    await waitFor(() => {
      expect(screen.getByText(/unexpected response/i)).toBeInTheDocument()
    })
  })

  it('shows error message when API call throws', async () => {
    MockedRiskEngineClient.prototype.calculateRisk.mockRejectedValue(
      new Error('Server error (500): Internal Server Error')
    )
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    await waitFor(() => {
      expect(screen.getByText(/server error \(500\)/i)).toBeInTheDocument()
    })
  })

  it('shows validation error when all percentages are zero', async () => {
    const user = userEvent.setup()

    render(<HomePage />)

    // Set all inputs to 0
    for (const code of Object.keys(DEFAULT_SCENARIOS)) {
      const input = screen.getByLabelText(`Percentage change for ${COUNTRY_NAMES[code]}`)
      await user.clear(input)
      await user.type(input, '0')
    }

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    expect(screen.getByText(/you must provide a non-zero percentage/i)).toBeInTheDocument()
    expect(MockedRiskEngineClient.prototype.calculateRisk).not.toHaveBeenCalled()
  })

  it('resets inputs to defaults when Reset Defaults is clicked', async () => {
    const user = userEvent.setup()

    render(<HomePage />)

    const firstCode = Object.keys(DEFAULT_SCENARIOS)[0]
    const input = screen.getByLabelText(`Percentage change for ${COUNTRY_NAMES[firstCode]}`) as HTMLInputElement

    await user.clear(input)
    await user.type(input, '99')
    expect(input.value).toBe('99')

    await user.click(screen.getByRole('button', { name: /reset defaults/i }))

    expect(input.value).toBe(DEFAULT_SCENARIOS[firstCode].toString())
  })

  it('disables buttons while loading', async () => {
    // Make calculateRisk hang so we can check the loading state
    let resolvePromise!: (value: number) => void
    MockedRiskEngineClient.prototype.calculateRisk.mockImplementation(
      () => new Promise(resolve => { resolvePromise = resolve })
    )
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    expect(screen.getByRole('button', { name: /running/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /reset defaults/i })).toBeDisabled()

    // Resolve to clean up
    resolvePromise(201)
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /run scenario/i })).toBeEnabled()
    })
  })

  it('clears previous messages when input changes', async () => {
    MockedRiskEngineClient.prototype.calculateRisk.mockResolvedValue(201)
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    await waitFor(() => {
      expect(screen.getByText(/risk calculations were successful/i)).toBeInTheDocument()
    })

    const firstCode = Object.keys(DEFAULT_SCENARIOS)[0]
    const input = screen.getByLabelText(`Percentage change for ${COUNTRY_NAMES[firstCode]}`)
    await user.clear(input)
    await user.type(input, '1')

    expect(screen.queryByText(/risk calculations were successful/i)).not.toBeInTheDocument()
  })

  it('sends correct request payload to calculateRisk', async () => {
    MockedRiskEngineClient.prototype.calculateRisk.mockResolvedValue(201)
    const user = userEvent.setup()

    render(<HomePage />)

    await user.click(screen.getByRole('button', { name: /run scenario/i }))

    await waitFor(() => {
      expect(MockedRiskEngineClient.prototype.calculateRisk).toHaveBeenCalledTimes(1)
    })

    const callArg = MockedRiskEngineClient.prototype.calculateRisk.mock.calls[0][0]
    expect(callArg.countryPercentageChanges).toBeDefined()

    for (const [code, val] of Object.entries(DEFAULT_SCENARIOS)) {
      expect(callArg.countryPercentageChanges[code]).toBe(val)
    }
  })
})
