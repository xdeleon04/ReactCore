import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { useAuth } from './useAuth';
import { AuthContext } from '../state/AuthContext';

const TestComponent = () => {
  useAuth();
  return <div>Auth Context Found</div>;
};

describe('useAuth', () => {
  it('throws error when used outside AuthProvider', () => {
    // Suppress console.error for this test as React logs the error
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => render(<TestComponent />)).toThrow('useAuth must be used within an AuthProvider');

    consoleSpy.mockRestore();
  });

  it('returns context when used within AuthProvider', () => {
    const mockContext = {
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
      login: vi.fn(),
      logout: vi.fn(),
      register: vi.fn(),
    };

    render(
      <AuthContext.Provider value={mockContext}>
        <TestComponent />
      </AuthContext.Provider>
    );

    expect(screen.getByText('Auth Context Found')).toBeInTheDocument();
  });
});
