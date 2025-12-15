import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { WeatherWidget } from '../WeatherWidget'

const queryMocks = vi.hoisted(() => ({
  useQuery: vi.fn(),
}))

const serviceMocks = vi.hoisted(() => ({
  fetchWeather: vi.fn(),
  transformWeatherData: vi.fn(),
}))

vi.mock('@tanstack/react-query', () => ({
  useQuery: queryMocks.useQuery,
}))

vi.mock('../../services/externalApiService', () => ({
  externalApiService: {
    fetchWeather: serviceMocks.fetchWeather,
    transformWeatherData: serviceMocks.transformWeatherData,
    getWeatherEmoji: vi.fn(),
  },
}))

describe('WeatherWidget', () => {
  beforeEach(() => {
    queryMocks.useQuery.mockReset()
    serviceMocks.fetchWeather.mockReset()
    serviceMocks.transformWeatherData.mockReset()
  })

  it('shows loading skeleton', () => {
    queryMocks.useQuery.mockReturnValue({
      isLoading: true,
      isError: false,
      data: undefined,
      error: null,
      refetch: vi.fn(),
    })

    const { container } = render(<WeatherWidget location="Santo Domingo" refreshIntervalMs={60_000} />)
    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0)
  })

  it('renders weather data when loaded', async () => {
    const raw = {
      temperature: 18,
      condition: 'Clear',
      location: 'Santo Domingo',
      humidity: 50,
      windSpeed: 3,
      fetchedAt: new Date().toISOString(),
      isCached: true,
      cacheExpiresAt: new Date(Date.now() + 60_000).toISOString(),
    }

    queryMocks.useQuery.mockReturnValue({
      isLoading: false,
      isError: false,
      data: raw,
      error: null,
      refetch: vi.fn(),
    })

    serviceMocks.transformWeatherData.mockReturnValue({
      temperature: 18,
      condition: 'Clear',
      location: 'Santo Domingo',
      emoji: '☀️',
      isCached: true,
      fetchedAt: raw.fetchedAt,
    })

    render(<WeatherWidget location="Santo Domingo" refreshIntervalMs={60_000} />)

    expect(await screen.findByText('Weather')).toBeInTheDocument()
    expect(screen.getByText('Santo Domingo')).toBeInTheDocument()
    expect(screen.getByText('Cached')).toBeInTheDocument()
    expect(screen.getByText(/18°C/i)).toBeInTheDocument()
  })

  it('renders error state and calls refetch on retry', async () => {
    const refetch = vi.fn()

    queryMocks.useQuery.mockReturnValue({
      isLoading: false,
      isError: true,
      data: undefined,
      error: new Error('Boom'),
      refetch,
    })

    render(<WeatherWidget location="Santo Domingo" refreshIntervalMs={60_000} />)

    expect(await screen.findByText('Unable to load weather.')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /retry/i }))
    expect(refetch).toHaveBeenCalledTimes(1)
  })

  it('passes refetchInterval to useQuery', () => {
    queryMocks.useQuery.mockReturnValue({
      isLoading: true,
      isError: false,
      data: undefined,
      error: null,
      refetch: vi.fn(),
    })

    render(<WeatherWidget location="Santo Domingo" refreshIntervalMs={1234} />)

    expect(queryMocks.useQuery).toHaveBeenCalledTimes(1)
    expect(queryMocks.useQuery.mock.calls[0][0].refetchInterval).toBe(1234)
  })
})
