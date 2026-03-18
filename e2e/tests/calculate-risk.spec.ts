import { test, expect } from '@playwright/test'

test.describe('POST /riskengine/calculate-risk', () => {
  test('returns 201 Created for a valid scenario request', async ({ request }) => {
    const response = await request.post('/riskengine/calculate-risk', {
      data: {
        countryPercentageChanges: {
          US: -4.34,
          GB: -5.12,
        },
      },
    })

    expect(response.status()).toBe(201)
  })

  test('returns 201 Created with a single country', async ({ request }) => {
    const response = await request.post('/riskengine/calculate-risk', {
      data: {
        countryPercentageChanges: {
          FR: -3.87,
        },
      },
    })

    expect(response.status()).toBe(201)
  })

  test('returns 201 Created with all supported countries', async ({ request }) => {
    const response = await request.post('/riskengine/calculate-risk', {
      data: {
        countryPercentageChanges: {
          GB: -5.12,
          US: -4.34,
          FR: -3.87,
          DE: -1.23,
          SG: -5.5,
          GR: -5.68,
        },
      },
    })

    expect(response.status()).toBe(201)
  })

  test('returns 400 Bad Request when countryPercentageChanges is empty', async ({ request }) => {
    const response = await request.post('/riskengine/calculate-risk', {
      data: {
        countryPercentageChanges: {},
      },
    })

    expect(response.status()).toBe(400)
    const body = await response.text()
    expect(body).toContain('At least one country percentage change is required.')
  })

  test('returns 400 Bad Request when body is missing countryPercentageChanges', async ({ request }) => {
    const response = await request.post('/riskengine/calculate-risk', {
      data: {},
    })

    expect(response.status()).toBe(400)
  })

  test('returns 400 Bad Request when body is invalid JSON', async ({ request }) => {
    const response = await request.fetch('/riskengine/calculate-risk', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      data: 'not-json',
    })

    expect(response.status()).toBe(400)
  })
})
