import React, { useEffect, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { externalApiService } from '../services/externalApiService'
import { WeatherWidgetSkeleton } from './skeletons/WeatherWidgetSkeleton'

interface WeatherWidgetProps {
  location?: string
  refreshIntervalMs?: number
  onError?: (error: Error) => void
}

export const WeatherWidget: React.FC<WeatherWidgetProps> = ({
  location = 'Santo Domingo',
  refreshIntervalMs = 5 * 60 * 1000,
  onError,
}) => {
  const query = useQuery({
    queryKey: ['weather', location],
    queryFn: () => externalApiService.fetchWeather(location),
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    refetchInterval: refreshIntervalMs,
    retry: 3,
    retryDelay: (attemptIndex) => Math.pow(2, attemptIndex) * 1000,
  })

  const display = useMemo(() => {
    return query.data ? externalApiService.transformWeatherData(query.data) : null
  }, [query.data])

  const errorForCallback = useMemo(() => {
    if (!query.isError) return null
    return query.error instanceof Error ? query.error : new Error('Unable to load weather')
  }, [query.error, query.isError])

  useEffect(() => {
    if (!errorForCallback) return
    onError?.(errorForCallback)
  }, [errorForCallback, onError])

  if (query.isLoading) return <WeatherWidgetSkeleton />

  if (query.isError) {
    return (
      <div className="rounded border border-gray-200 p-3">
        <div className="text-sm font-semibold text-gray-900">Weather</div>
        <p className="mt-2 text-sm text-gray-700">Unable to load weather.</p>
        <button
          type="button"
          className="mt-3 rounded bg-gray-900 px-4 py-2 text-sm font-medium text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
          onClick={() => query.refetch()}
        >
          Retry
        </button>
      </div>
    )
  }

  if (!display) return null

  return (
    <div className="rounded border border-gray-200 p-3">
      <div className="flex items-center justify-between gap-3">
        <div>
          <div className="text-sm font-semibold text-gray-900">Weather</div>
          <div className="text-sm text-gray-700">{display.location}</div>
        </div>
        <div className="text-sm text-gray-700">{display.isCached ? 'Cached' : 'Live'}</div>
      </div>

      <div className="mt-3 flex items-baseline gap-2">
        <div className="text-3xl font-semibold text-gray-900">{display.temperature}°C</div>
        <div className="text-sm text-gray-700">
          {display.emoji} {display.condition}
        </div>
      </div>

      <div className="mt-2 text-xs text-gray-600">
        Updated {new Date(display.fetchedAt).toLocaleTimeString()}
      </div>
    </div>
  )
}
