import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { CheckoutForm } from './CheckoutForm';

const navigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<any>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => navigate,
  };
});

const clear = vi.fn();

vi.mock('../hooks/useCart', () => ({
  useCart: () => ({
    items: [{ productId: 1, productName: 'Widget', unitPrice: 10, imageUrl: null, quantity: 1 }],
    subtotal: 10,
    clear,
    updateQuantity: vi.fn(),
    removeItem: vi.fn(),
  }),
}));

const getCart = vi.fn();
vi.mock('../services/cartService', () => ({
  getCart: () => getCart(),
}));

const createOrder = vi.fn();
vi.mock('../services/orderService', () => ({
  createOrder: (req: any) => createOrder(req),
  isCreateOrderConflict: () => false,
}));

describe('CheckoutForm', () => {
  beforeEach(() => {
    navigate.mockReset();
    clear.mockReset();
    getCart.mockReset();
    createOrder.mockReset();
  });

  it('disables submit for invalid email', () => {
    render(<CheckoutForm />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'not-an-email' } });
    expect(screen.getByRole('button', { name: /place order/i })).toBeDisabled();
  });

  it('creates order and navigates on success', async () => {
    getCart.mockResolvedValue({ id: 123, items: [] });
    createOrder.mockResolvedValue({ orderNumber: 'ORD-20251214-001' });

    render(<CheckoutForm />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'customer@example.com' } });

    const button = screen.getByRole('button', { name: /place order/i });
    expect(button).not.toBeDisabled();

    fireEvent.click(button);

    await waitFor(() => {
      expect(createOrder).toHaveBeenCalledWith({ email: 'customer@example.com', cartId: 123 });
      expect(clear).toHaveBeenCalled();
      expect(navigate).toHaveBeenCalledWith('/orders/ORD-20251214-001');
    });
  });
});
