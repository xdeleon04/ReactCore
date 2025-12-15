import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { AuditLogsPage } from './AuditLogs';

vi.mock('../../services/admin/adminAuditLogService', () => ({
  listAuditLogs: vi.fn(),
}));

import { listAuditLogs } from '../../services/admin/adminAuditLogService';

const listAuditLogsMock = vi.mocked(listAuditLogs);

describe('AuditLogsPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders audit log table rows', async () => {
    listAuditLogsMock.mockResolvedValue({
      items: [
        {
          id: 1,
          adminEmail: 'admin@example.com',
          action: 'ProductUpdate',
          entityType: 'Product',
          entityId: '1',
          timestamp: new Date('2025-01-07T00:00:00Z').toISOString(),
          oldValues: '{"price": 10}',
          newValues: '{"price": 9}',
          reason: 'Q4 price adjustment',
        },
      ],
      total: 1,
      skip: 0,
      take: 50,
    });

    render(<AuditLogsPage />);

    expect(await screen.findByText('Admin · Audit Logs')).toBeInTheDocument();
    expect(await screen.findByText('admin@example.com')).toBeInTheDocument();
    expect(screen.getByText('ProductUpdate')).toBeInTheDocument();
    expect(screen.getByText('Q4 price adjustment')).toBeInTheDocument();
    expect(screen.getByText('Total: 1')).toBeInTheDocument();

    await waitFor(() => expect(listAuditLogsMock).toHaveBeenCalled());
  });

  it('applies filters and calls listAuditLogs with ISO params', async () => {
    listAuditLogsMock.mockResolvedValue({ items: [], total: 0, skip: 0, take: 50 });

    render(<AuditLogsPage />);

    await waitFor(() => expect(listAuditLogsMock).toHaveBeenCalled());

    await waitFor(() => {
      const apply = screen.getByRole('button', { name: 'Apply' });
      expect(apply).not.toBeDisabled();
    });

    fireEvent.change(screen.getByLabelText('Start date'), { target: { value: '2025-01-02' } });
    fireEvent.change(screen.getByLabelText('End date'), { target: { value: '2025-01-03' } });

    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));

    await waitFor(() => {
      const lastCall = listAuditLogsMock.mock.calls[listAuditLogsMock.mock.calls.length - 1];
      expect(lastCall).toBeTruthy();
      const params = lastCall?.[0];
      expect(params.startDate).toBe('2025-01-02T00:00:00.000Z');
      expect(params.endDate).toBe('2025-01-03T23:59:59.000Z');
      expect(params.skip).toBe(0);
      expect(params.take).toBe(50);
    });
  });
});
