import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { vi, describe, it, expect, type Mock } from 'vitest';
import { useAuth } from '../hooks/useAuth';

// Mock useAuth hook
vi.mock('../hooks/useAuth');

describe('ProtectedRoute', () => {
  it('renders loading state when loading', () => {
    (useAuth as Mock).mockReturnValue({
      isAuthenticated: false,
      isLoading: true,
    });

    render(
      <MemoryRouter>
        <ProtectedRoute />
      </MemoryRouter>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('redirects to login when not authenticated (e.g. after logout)', () => {
    (useAuth as Mock).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route path="/protected" element={<ProtectedRoute />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  it('renders children when authenticated', () => {
    (useAuth as Mock).mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('renders outlet when authenticated and no children provided', () => {
    (useAuth as Mock).mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute />}>
             <Route index element={<div>Outlet Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Outlet Content')).toBeInTheDocument();
  });
});
