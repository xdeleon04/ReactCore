import '@testing-library/jest-dom'
import { expect, afterEach, beforeAll, afterAll, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { server } from './mocks/server'
import { toHaveNoViolations } from 'jest-axe'

expect.extend(toHaveNoViolations)

/**
 * ============ MSW Setup ============
 * Mock Service Worker automatically intercepts API calls
 */

beforeAll(() => {
  server.listen({
    onUnhandledRequest: 'error', // Fail tests on unexpected API calls
  })
})

afterEach(() => {
  cleanup()
  server.resetHandlers()
  vi.clearAllMocks()
})

afterAll(() => {
  server.close()
})

/**
 * ============ DOM API Mocks ============
 * Mock browser APIs that may not be available in jsdom
 */

// Mock window.matchMedia for responsive design queries
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated but may be called by old code
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock IntersectionObserver for lazy loading and virtualization
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return []
  }
  unobserve() {}
} as unknown as typeof IntersectionObserver

// Mock ResizeObserver for responsive components
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
} as unknown as typeof ResizeObserver

// Mock scrollIntoView which doesn't work in jsdom
Element.prototype.scrollIntoView = vi.fn()

/**
 * ============ Window APIs ============
 */

// Mock localStorage for auth tokens and preferences
const localStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
})

// Mock sessionStorage
const sessionStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'sessionStorage', {
  value: sessionStorageMock,
})

// Mock scrollTo
window.scrollTo = vi.fn()

/**
 * ============ Console Suppression ============
 * Optionally suppress console warnings in tests
 * Remove if you want to see all console output
 */

const originalError = console.error
const originalWarn = console.warn

beforeAll(() => {
  // Suppress React 19 specific warnings if needed
  console.error = (...args: unknown[]) => {
    const message = args[0]?.toString() || ''

    // Suppress specific known warnings
    if (
      message.includes('Warning: useLayoutEffect') ||
      message.includes('Warning: ReactDOM.render')
    ) {
      return
    }

    originalError.call(console, ...args)
  }

  console.warn = (...args: unknown[]) => {
    const message = args[0]?.toString() || ''

    // Suppress specific known warnings
    if (message.includes('Warning:')) {
      return
    }

    originalWarn.call(console, ...args)
  }
})

afterAll(() => {
  console.error = originalError
  console.warn = originalWarn
})
