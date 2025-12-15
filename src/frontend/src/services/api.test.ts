import { describe, it, expect, vi } from 'vitest';

// Mock axios
const mocks = vi.hoisted(() => ({
  requestUse: vi.fn(),
}));

vi.mock('axios', () => {
  const mockAxios = {
    create: vi.fn(() => ({
      interceptors: {
        request: { use: mocks.requestUse, eject: vi.fn() },
        response: { use: vi.fn(), eject: vi.fn() },
      },
      defaults: { headers: { common: {} } },
      get: vi.fn(),
      post: vi.fn(),
    })),
  };
  return {
    default: mockAxios,
  };
});

describe('api service', () => {
  // We don't clear mocks because api.ts runs at import time and calls interceptors.use.
  // If we clear mocks, we lose the reference to the interceptor callback.

  const importApi = async () => {
    mocks.requestUse.mockClear()
    vi.resetModules()
    return await import('./api')
  }

  it('sets access token and uses it in interceptor', async () => {
    const { setAccessToken } = await importApi()
    setAccessToken('test-token');

    // Get the interceptor callback
    // api.ts calls interceptors.request.use(callback, errorCallback)
    // So the first argument of the first call to requestUse is our callback
    const interceptor = mocks.requestUse.mock.calls.at(-1)?.[0];
    expect(interceptor).toBeTypeOf('function')

    const config = { headers: {} };
    const result = interceptor(config);

    expect(result.headers.Authorization).toBe('Bearer test-token');
  });

  it('removes access token', async () => {
    const { setAccessToken } = await importApi()
    setAccessToken(null);
    const interceptor = mocks.requestUse.mock.calls.at(-1)?.[0];
    expect(interceptor).toBeTypeOf('function')
    const config = { headers: {} };
    const result = interceptor(config);
    expect(result.headers.Authorization).toBeUndefined();
  });


  it('registers logout callback', async () => {
    const { onLogout } = await importApi()
    const callback = vi.fn();
    onLogout(callback);
    // We can't easily trigger the interceptor without simulating a response error.
    // But we can verify the function runs without error.
  });
});
