export interface WeatherData {
  temperature: number
  condition: string
  location: string
  humidity: number
  windSpeed: number
  fetchedAt: string
  isCached: boolean
  cacheExpiresAt: string
}

export interface WeatherDisplay {
  temperature: number
  condition: string
  location: string
  emoji: string
  isCached: boolean
  fetchedAt: string
}
