import { describe, it, expect, vi, beforeEach } from 'vitest'

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
}))

vi.mock('../api', () => ({
  default: {
    get: mocks.get,
  },
}))

import { externalApiService } from '../externalApiService'

describe('externalApiService', () => {
  beforeEach(() => {
    mocks.get.mockReset()
  })

  it('fetchWeather calls backend endpoint with location param', async () => {
    const dto = {
      temperature: 10,
      condition: 'Clear',
      location: 'Santo Domingo',
      humidity: 50,
      windSpeed: 3,
      fetchedAt: new Date().toISOString(),
      isCached: false,
      cacheExpiresAt: new Date(Date.now() + 60_000).toISOString(),
    }

    mocks.get.mockResolvedValue({ data: dto })

    const result = await externalApiService.fetchWeather('Santo Domingo')

    expect(mocks.get).toHaveBeenCalledWith('/external/weather', { params: { location: 'Santo Domingo' } })
    expect(result).toEqual(dto)
  })

  it('transformWeatherData rounds temperature and maps emoji', () => {
    const raw = {
      temperature: 18.6,
      condition: 'Clear',
      location: 'Santo Domingo',
      humidity: 50,
      windSpeed: 3,
      fetchedAt: new Date().toISOString(),
      isCached: true,
      cacheExpiresAt: new Date(Date.now() + 60_000).toISOString(),
    }

    const display = externalApiService.transformWeatherData(raw)

    expect(display.temperature).toBe(19)
    expect(display.location).toBe('Santo Domingo')
    expect(display.emoji).toBe('☀️')
    expect(display.isCached).toBe(true)
  })

  it('getWeatherEmoji returns default for unknown conditions', () => {
    expect(externalApiService.getWeatherEmoji('Volcanic Ash')).toBe('🌤️')
  })
})
