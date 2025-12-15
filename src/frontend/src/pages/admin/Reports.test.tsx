import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import { ReportsPage } from './Reports';

vi.mock('../../services/admin/adminReportService', () => ({
  getSalesReport: vi.fn(),
  exportSalesReportCsv: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

import { getSalesReport, exportSalesReportCsv } from '../../services/admin/adminReportService';

const getSalesReportMock = vi.mocked(getSalesReport);
const exportSalesReportCsvMock = vi.mocked(exportSalesReportCsv);

describe('ReportsPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:mock');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    getSalesReportMock.mockResolvedValue({
      dateRange: {
        start: '2024-12-01T00:00:00Z',
        end: '2024-12-31T23:59:59Z',
      },
      summary: {
        totalRevenue: 140,
        totalOrders: 2,
        averageOrderValue: 70,
        totalItemsSold: 4,
        uniqueCustomers: 2,
      },
      topProducts: [
        { productId: 1, productName: 'Widget', unitsSold: 2, revenue: 100 },
        { productId: 2, productName: 'Cable', unitsSold: 2, revenue: 40 },
      ],
      orderStatusBreakdown: [
        { status: 'Processing', count: 1, percentage: 50 },
        { status: 'Completed', count: 1, percentage: 50 },
      ],
      generatedAt: '2024-12-31T12:00:00Z',
    });

    exportSalesReportCsvMock.mockResolvedValue(new Blob(['OrderNumber,Date,...'], { type: 'text/csv' }));
  });

  it('renders summary metrics', async () => {
    render(<ReportsPage />);

    expect(await screen.findByText('Admin · Reports')).toBeInTheDocument();

    await waitFor(() => expect(getSalesReportMock).toHaveBeenCalled());

    const summary = screen.getByRole('region', { name: 'Report summary' });
    expect(await within(summary).findByText('$140.00')).toBeInTheDocument();
    const totalOrdersLabel = within(summary).getByText('Total orders');
    const totalOrdersCard = totalOrdersLabel.parentElement;
    expect(totalOrdersCard).not.toBeNull();
    expect(within(totalOrdersCard as HTMLElement).getByText('2')).toBeInTheDocument();
  });

  it('exports CSV when clicking Export', async () => {
    render(<ReportsPage />);

    await waitFor(() => expect(getSalesReportMock).toHaveBeenCalled());

    fireEvent.click(screen.getByRole('button', { name: 'Export CSV' }));

    await waitFor(() => expect(exportSalesReportCsvMock).toHaveBeenCalled());
  });
});
