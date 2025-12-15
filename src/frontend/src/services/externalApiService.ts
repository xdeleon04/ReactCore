import api from './api'
import type { WeatherData, WeatherDisplay } from '../types/weather'

export const externalApiService = {
  async fetchWeather(location: string = 'Santo Domingo'): Promise<WeatherData> {
    const response = await api.get<WeatherData>('/external/weather', {
      params: { location },
    })

    return response.data
  },

  transformWeatherData(raw: WeatherData): WeatherDisplay {
    return {
      temperature: Math.round(raw.temperature),
      condition: raw.condition,
      location: raw.location,
      emoji: this.getWeatherEmoji(raw.condition),
      isCached: raw.isCached,
      fetchedAt: raw.fetchedAt,
    }
  },

  getWeatherEmoji(condition: string): string {
    const normalized = condition.trim().toLowerCase()

    const emojis: Record<string, string> = {
      sunny: '☀️',
      clear: '☀️',
      clouds: '☁️',
      cloudy: '☁️',
      rain: '🌧️',
      rainy: '🌧️',
      drizzle: '🌦️',
      thunderstorm: '⛈️',
      snow: '❄️',
      mist: '🌫️',
      haze: '🌫️',
      fog: '🌫️',
    }

    return emojis[normalized] ?? '🌤️'
  },
}
