import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { AddToCartButton } from './AddToCartButton';

const addItem = vi.fn();

vi.mock('../hooks/useCart', () => ({
  useCart: () => ({
    addItem,
  }),
}));

describe('AddToCartButton', () => {
  beforeEach(() => {
    addItem.mockReset();
  });

  it('disables when out-of-stock', () => {
    render(
      <AddToCartButton
        product={{ id: 1, name: 'Test', price: 10, imageUrl: null, status: 'out-of-stock' }}
      />
    );

    expect(screen.getByRole('button', { name: /add to cart/i })).toBeDisabled();
  });

  it('calls addItem with quantity', () => {
    render(
      <AddToCartButton
        product={{ id: 2, name: 'Widget', price: 12.5, imageUrl: null, status: 'in-stock' }}
      />
    );

    fireEvent.change(screen.getByLabelText(/qty/i), { target: { value: '3' } });
    fireEvent.click(screen.getByRole('button', { name: /add to cart/i }));

    expect(addItem).toHaveBeenCalledTimes(1);
    expect(addItem).toHaveBeenCalledWith(
      {
        productId: 2,
        productName: 'Widget',
        unitPrice: 12.5,
        imageUrl: null,
      },
      3
    );
  });
});
