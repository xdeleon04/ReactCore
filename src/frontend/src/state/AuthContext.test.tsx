import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import { AuthProvider, AuthContext } from './AuthContext';
import api, { setAccessToken } from '../services/api';
import { jwtDecode } from 'jwt-decode';
import { vi, describe, it, expect, beforeEach, afterEach, type Mock } from 'vitest';

// Mock dependencies
vi.mock('../services/api', async () => {
  const actual = await vi.importActual('../services/api');
  return {
    ...actual,
    default: {
      post: vi.fn(),
    },
    setAccessToken: vi.fn(),
    onLogout: vi.fn(),
  };
});

vi.mock('jwt-decode', () => ({
  jwtDecode: vi.fn(),
}));

const TestComponent = () => {
  const { user, isAuthenticated, login, logout, error, isLoading } = React.useContext(AuthContext)!;
  return (
    <div>
      <div data-testid="loading">{isLoading.toString()}</div>
      <div data-testid="auth">{isAuthenticated.toString()}</div>
      <div data-testid="user">{user?.email}</div>
      <div data-testid="error">{error}</div>
      <button onClick={() => login('test@example.com', 'password').catch(() => {})}>Login</button>
      <button onClick={logout}>Logout</button>
    </div>
  );
};

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });


  it('initializes with loading state and tries silent refresh', async () => {
    (api.post as Mock).mockRejectedValue(new Error('No session'));

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    expect(screen.getByTestId('loading')).toHaveTextContent('true');

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('false');
    });

    expect(api.post).toHaveBeenCalledWith('/auth/refresh');
  });

  it('logs in successfully', async () => {
    (api.post as Mock).mockResolvedValueOnce({ data: { accessToken: 'fake-token' } }); // for silent refresh (fail)
    (api.post as Mock).mockRejectedValueOnce(new Error('No session'));

    // Wait for init
    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));

    // Setup login mocks
    (api.post as Mock).mockResolvedValueOnce({ data: { accessToken: 'new-token' } });
    (jwtDecode as Mock).mockReturnValue({
      email: 'user@example.com',
      sub: '123',
      role: 'user',
      exp: Date.now() / 1000 + 3600
    });

    // Act
    await act(async () => {
      screen.getByText('Login').click();
    });

    // Assert
    expect(api.post).toHaveBeenCalledWith('/auth/login', { email: 'test@example.com', password: 'password' });
    expect(setAccessToken).toHaveBeenCalledWith('new-token');
    expect(screen.getByTestId('user')).toHaveTextContent('user@example.com');
    expect(screen.getByTestId('auth')).toHaveTextContent('true');
  });

  it('handles login failure', async () => {
    (api.post as Mock).mockRejectedValueOnce(new Error('No session')); // init

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );
    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));

    // Setup login failure
    (api.post as Mock).mockRejectedValueOnce({ response: { data: { message: 'Invalid credentials' } } });

    // Act
    await act(async () => {
      screen.getByText('Login').click();
    });

    // Assert
    expect(screen.getByTestId('error')).toHaveTextContent('Invalid credentials');
  });

  it('logs out successfully', async () => {
    (api.post as Mock).mockRejectedValueOnce(new Error('No session')); // init

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );
    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));

    // Act
    await act(async () => {
      screen.getByText('Logout').click();
    });

    // Assert
    expect(api.post).toHaveBeenCalledWith('/auth/logout');
    expect(setAccessToken).toHaveBeenCalledWith(null);
  });
});
