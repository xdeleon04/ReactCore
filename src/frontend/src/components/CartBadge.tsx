import React from 'react';
import { useCart } from '../hooks/useCart';

export const CartBadge: React.FC<{ onClick: () => void }> = ({ onClick }) => {
  const { itemCount } = useCart();

  return (
    <button
      type="button"
      className="relative rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
      onClick={onClick}
      aria-label="Open cart"
    >
      Cart
      {itemCount > 0 ? (
        <span className="absolute -right-2 -top-2 rounded-full bg-gray-900 px-2 py-0.5 text-xs text-white">
          {itemCount}
        </span>
      ) : null}
    </button>
  );
};
