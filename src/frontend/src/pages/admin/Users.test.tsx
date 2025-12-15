import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { AdminUsersPage } from './Users';

vi.mock('../../services/admin/adminUserService', () => ({
  listUsers: vi.fn(),
  deactivateUser: vi.fn(),
  reactivateUser: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

import { listUsers, deactivateUser, reactivateUser } from '../../services/admin/adminUserService';

const listUsersMock = vi.mocked(listUsers);
const deactivateUserMock = vi.mocked(deactivateUser);
const reactivateUserMock = vi.mocked(reactivateUser);

describe('AdminUsersPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders table with users', async () => {
    listUsersMock.mockResolvedValue({
      items: [
        {
          id: 'u1',
          email: 'a@example.com',
          role: 'user',
          isActive: true,
          createdAt: new Date('2024-01-01').toISOString(),
          lastLogin: null,
        },
      ],
      total: 1,
      skip: 0,
      take: 20,
    });

    render(<AdminUsersPage />);

    expect(await screen.findByText('a@example.com')).toBeInTheDocument();
    expect(screen.getByText('Deactivate')).toBeInTheDocument();
  });

  it('submits search filters and calls listUsers with email', async () => {
    listUsersMock.mockResolvedValue({ items: [], total: 0, skip: 0, take: 20 });

    render(<AdminUsersPage />);

    // Wait for initial load to finish so the submit button is enabled and labeled "Search"
    await waitFor(() => expect(listUsersMock).toHaveBeenCalled());
    const submit = await screen.findByRole('button', { name: /search/i });
    await waitFor(() => expect(submit).not.toBeDisabled());

    const input = await screen.findByLabelText('Email');
    fireEvent.change(input, { target: { value: 'john' } });
    fireEvent.submit(screen.getByRole('form', { name: 'User filters' }));

    await waitFor(() => {
      expect(listUsersMock).toHaveBeenCalled();
      const params = listUsersMock.mock.calls[listUsersMock.mock.calls.length - 1]?.[0];
      expect(params.email).toBe('john');
    });
  });

  it('deactivates active user and refreshes list', async () => {
    listUsersMock
      .mockResolvedValueOnce({
        items: [
          {
            id: 'u1',
            email: 'a@example.com',
            role: 'user',
            isActive: true,
            createdAt: new Date('2024-01-01').toISOString(),
            lastLogin: null,
          },
        ],
        total: 1,
        skip: 0,
        take: 20,
      })
      .mockResolvedValueOnce({
        items: [
          {
            id: 'u1',
            email: 'a@example.com',
            role: 'user',
            isActive: false,
            createdAt: new Date('2024-01-01').toISOString(),
            lastLogin: null,
          },
        ],
        total: 1,
        skip: 0,
        take: 20,
      });

    deactivateUserMock.mockResolvedValue({ userId: 'u1', isActive: false });

    render(<AdminUsersPage />);

    expect(await screen.findByText('a@example.com')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Deactivate' }));

    await waitFor(() => expect(deactivateUserMock).toHaveBeenCalledWith('u1'));
    await waitFor(() => expect(screen.getByText('Reactivate')).toBeInTheDocument());
  });

  it('reactivates inactive user and refreshes list', async () => {
    listUsersMock
      .mockResolvedValueOnce({
        items: [
          {
            id: 'u1',
            email: 'a@example.com',
            role: 'user',
            isActive: false,
            createdAt: new Date('2024-01-01').toISOString(),
            lastLogin: null,
          },
        ],
        total: 1,
        skip: 0,
        take: 20,
      })
      .mockResolvedValueOnce({
        items: [
          {
            id: 'u1',
            email: 'a@example.com',
            role: 'user',
            isActive: true,
            createdAt: new Date('2024-01-01').toISOString(),
            lastLogin: null,
          },
        ],
        total: 1,
        skip: 0,
        take: 20,
      });

    reactivateUserMock.mockResolvedValue({ userId: 'u1', isActive: true });

    render(<AdminUsersPage />);

    expect(await screen.findByText('a@example.com')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Reactivate' }));

    await waitFor(() => expect(reactivateUserMock).toHaveBeenCalledWith('u1'));
    await waitFor(() => expect(screen.getByText('Deactivate')).toBeInTheDocument());
  });
});
