import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { OrdersPage } from './Orders';

vi.mock('../../services/admin/adminOrderService', () => ({
  listOrders: vi.fn(),
  getOrderDetail: vi.fn(),
  updateOrderStatus: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

import { listOrders, getOrderDetail, updateOrderStatus } from '../../services/admin/adminOrderService';

const listOrdersMock = vi.mocked(listOrders);
const getOrderDetailMock = vi.mocked(getOrderDetail);
const updateOrderStatusMock = vi.mocked(updateOrderStatus);

describe('OrdersPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders table with orders', async () => {
    listOrdersMock.mockResolvedValue({
      items: [
        {
          id: 1,
          orderNumber: 'ORD-1001',
          customerEmail: 'customer@example.com',
          total: 42.5,
          status: 'Pending',
          createdAt: new Date('2025-01-01T00:00:00Z').toISOString(),
        },
      ],
      total: 1,
      skip: 0,
      take: 20,
    });

    render(<OrdersPage />);

    expect(await screen.findByText('ORD-1001')).toBeInTheDocument();
    expect(screen.getByText('customer@example.com')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'View' })).toBeInTheDocument();
  });

  it('submits filters and calls listOrders with orderNumber and status', async () => {
    listOrdersMock.mockResolvedValue({ items: [], total: 0, skip: 0, take: 20 });

    render(<OrdersPage />);

    await waitFor(() => expect(listOrdersMock).toHaveBeenCalled());

    const submit = await screen.findByRole('button', { name: /search/i });
    await waitFor(() => expect(submit).not.toBeDisabled());

    fireEvent.change(screen.getByLabelText('Order #'), { target: { value: 'ORD-99' } });
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Pending' } });

    fireEvent.submit(screen.getByRole('form', { name: 'Order filters' }));

    await waitFor(() => {
      const lastCall = listOrdersMock.mock.calls[listOrdersMock.mock.calls.length - 1];
      expect(lastCall).toBeTruthy();
      const params = lastCall?.[0];

      expect(params.orderNumber).toBe('ORD-99');
      expect(params.status).toBe('Pending');
      expect(params.skip).toBe(0);
    });
  });

  it('opens detail modal and surfaces status update errors', async () => {
    listOrdersMock.mockResolvedValue({
      items: [
        {
          id: 1,
          orderNumber: 'ORD-1002',
          customerEmail: 'customer@example.com',
          total: 10,
          status: 'Pending',
          createdAt: new Date('2025-01-01T00:00:00Z').toISOString(),
        },
      ],
      total: 1,
      skip: 0,
      take: 20,
    });

    getOrderDetailMock.mockResolvedValue({
      id: 1,
      orderNumber: 'ORD-1002',
      customerEmail: 'customer@example.com',
      status: 'Pending',
      createdAt: new Date('2025-01-01T00:00:00Z').toISOString(),
      subtotal: 10,
      total: 10,
      items: [
        {
          productId: 99,
          productName: 'Widget',
          quantity: 1,
          unitPrice: 10,
          lineTotal: 10,
        },
      ],
      allowedStatusTransitions: ['Processing'],
    });

    updateOrderStatusMock.mockRejectedValue({
      response: {
        data: {
          message: 'Invalid transition',
        },
      },
    });

    render(<OrdersPage />);

    expect(await screen.findByText('ORD-1002')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'View' }));

    expect(await screen.findByText('Order detail')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Update status' }));

    await waitFor(() => {
      expect(updateOrderStatusMock).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          status: 'Processing',
          reason: undefined,
        }),
      );
    });

    expect(await screen.findByText('Invalid transition')).toBeInTheDocument();
  });
});
