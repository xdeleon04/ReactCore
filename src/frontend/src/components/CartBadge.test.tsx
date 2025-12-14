import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CartBadge } from './CartBadge';

vi.mock('../hooks/useCart', () => ({
  useCart: () => ({
    itemCount: 3,
  }),
}));

describe('CartBadge', () => {
  it('shows item count and fires click', () => {
    const onClick = vi.fn();
    render(<CartBadge onClick={onClick} />);

    expect(screen.getByText('Cart')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /open cart/i }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });
});
