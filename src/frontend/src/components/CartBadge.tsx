import React from 'react';
import { useCart } from '../hooks/useCart';

export const CartBadge: React.FC<{ onClick: () => void }> = ({ onClick }) => {
  const { itemCount } = useCart();

  return (
    <button
      type="button"
      className="relative rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 transition duration-300 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50 focus-visible:ring-offset-2"
      onClick={onClick}
      aria-label="Open cart"
    >
      Cart
      {itemCount > 0 ? (
        <span className="absolute -right-2 -top-2 rounded-full bg-slate-900 px-2 py-0.5 text-xs text-white">
          {itemCount}
        </span>
      ) : null}
    </button>
  );
};
