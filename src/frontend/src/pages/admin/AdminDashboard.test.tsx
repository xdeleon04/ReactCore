import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { AdminDashboardPage } from './AdminDashboard';

vi.mock('../../services/admin/adminDashboardService', () => ({
  getDashboardSummary: vi.fn(),
}));

import { getDashboardSummary } from '../../services/admin/adminDashboardService';

const getDashboardSummaryMock = vi.mocked(getDashboardSummary);

describe('AdminDashboardPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders summary metrics', async () => {
    getDashboardSummaryMock.mockResolvedValue({
      userMetrics: { totalUsers: 10, activeUsers: 9, inactiveUsers: 1, newUsersThisMonth: 2 },
      productMetrics: { totalProducts: 5, activeProducts: 4, archivedProducts: 1, lowStockCount: 1 },
      orderMetrics: { pendingOrders: 3, processingOrders: 2, totalOrdersToday: 7, todayRevenue: 123.45 },
    });

    render(<AdminDashboardPage />);

    expect(await screen.findByText('Admin · Dashboard')).toBeInTheDocument();
    expect(await screen.findByText('10')).toBeInTheDocument();
    expect(screen.getByText('123.45')).toBeInTheDocument();
  });

  it('refresh button refetches', async () => {
    getDashboardSummaryMock.mockResolvedValue({
      userMetrics: { totalUsers: 1, activeUsers: 1, inactiveUsers: 0, newUsersThisMonth: 1 },
      productMetrics: { totalProducts: 1, activeProducts: 1, archivedProducts: 0, lowStockCount: 0 },
      orderMetrics: { pendingOrders: 0, processingOrders: 0, totalOrdersToday: 0, todayRevenue: 0 },
    });

    render(<AdminDashboardPage />);

    await waitFor(() => expect(getDashboardSummaryMock).toHaveBeenCalledTimes(1));

    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));

    await waitFor(() => expect(getDashboardSummaryMock).toHaveBeenCalledTimes(2));
  });
});
