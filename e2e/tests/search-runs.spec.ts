import { test, expect } from '@playwright/test'

test.describe('GET /runssearch/search-runs', () => {
  test('returns 200 with paged results after a calculation run', async ({ request }) => {
    // First, create a run so there's data to search
    const createResponse = await request.post('/riskengine/calculate-risk', {
      data: {
        countryPercentageChanges: { US: -4.34 },
      },
    })
    expect(createResponse.status()).toBe(201)

    // Now search for runs
    const response = await request.get('/runssearch/search-runs?pageNumber=1&pageSize=10')

    expect(response.status()).toBe(200)

    const body = await response.json()
    expect(body.pageNumber).toBe(1)
    expect(body.pageSize).toBe(10)
    expect(body.totalCount).toBeGreaterThanOrEqual(1)
    expect(body.runs.length).toBeGreaterThanOrEqual(1)

    // Verify run structure
    const run = body.runs[0]
    expect(run).toHaveProperty('runDate')
    expect(run).toHaveProperty('timeTakenMs')
    expect(run).toHaveProperty('countryPercentageChanges')
    expect(run).toHaveProperty('portfolioResults')
    expect(Array.isArray(run.portfolioResults)).toBe(true)
  })

  test('returns correct page size when multiple runs exist', async ({ request }) => {
    // Create two runs
    await request.post('/riskengine/calculate-risk', {
      data: { countryPercentageChanges: { GB: -5.12 } },
    })
    await request.post('/riskengine/calculate-risk', {
      data: { countryPercentageChanges: { FR: -3.87 } },
    })

    // Request page size of 1
    const response = await request.get('/runssearch/search-runs?pageNumber=1&pageSize=1')

    expect(response.status()).toBe(200)

    const body = await response.json()
    expect(body.pageSize).toBe(1)
    expect(body.runs.length).toBe(1)
    expect(body.totalCount).toBeGreaterThanOrEqual(2)
  })

  test('returns 200 with empty runs when page exceeds total', async ({ request }) => {
    const response = await request.get('/runssearch/search-runs?pageNumber=9999&pageSize=10')

    expect(response.status()).toBe(200)

    const body = await response.json()
    expect(body.runs.length).toBe(0)
  })

  test('returns 400 Bad Request when pageNumber is 0', async ({ request }) => {
    const response = await request.get('/runssearch/search-runs?pageNumber=0&pageSize=10')

    expect(response.status()).toBe(400)
    const body = await response.text()
    expect(body).toContain('PageNumber must be >= 1 and PageSize must be between 1 and 100.')
  })

  test('returns 400 Bad Request when pageSize exceeds 100', async ({ request }) => {
    const response = await request.get('/runssearch/search-runs?pageNumber=1&pageSize=200')

    expect(response.status()).toBe(400)
    const body = await response.text()
    expect(body).toContain('PageNumber must be >= 1 and PageSize must be between 1 and 100.')
  })

  test('returns 400 Bad Request when pageSize is 0', async ({ request }) => {
    const response = await request.get('/runssearch/search-runs?pageNumber=1&pageSize=0')

    expect(response.status()).toBe(400)
  })

  test('portfolio results contain expected financial fields', async ({ request }) => {
    // Create a run
    await request.post('/riskengine/calculate-risk', {
      data: { countryPercentageChanges: { US: -4.34, GB: -5.12 } },
    })

    const response = await request.get('/runssearch/search-runs?pageNumber=1&pageSize=1')
    expect(response.status()).toBe(200)

    const body = await response.json()
    expect(body.runs.length).toBeGreaterThanOrEqual(1)

    const portfolioResult = body.runs[0].portfolioResults[0]
    expect(portfolioResult).toHaveProperty('portfolioId')
    expect(portfolioResult).toHaveProperty('portfolioName')
    expect(portfolioResult).toHaveProperty('country')
    expect(portfolioResult).toHaveProperty('currency')
    expect(portfolioResult).toHaveProperty('totalOutstandingAmount')
    expect(portfolioResult).toHaveProperty('totalCollateralValue')
    expect(portfolioResult).toHaveProperty('totalScenarioCollateralValue')
    expect(portfolioResult).toHaveProperty('totalExpectedLoss')
    expect(typeof portfolioResult.totalExpectedLoss).toBe('number')
  })
})
