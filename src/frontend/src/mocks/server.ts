import { setupServer } from 'msw/node'
import { handlers } from './handlers'

/**
 * Mock Service Worker (MSW) server configuration
 * Automatically intercepts and mocks API calls during tests
 *
 * Set up in setupTests.ts with beforeAll/afterAll lifecycle hooks
 */

export const server = setupServer(...handlers)

/**
 * Example usage in setupTests.ts:
 *
 * beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
 * afterEach(() => server.resetHandlers())
 * afterAll(() => server.close())
 *
 * Override specific handler in a test:
 *
 * server.use(
 *   http.get('/api/products', () => {
 *     return HttpResponse.json([])
 *   })
 * )
 */
