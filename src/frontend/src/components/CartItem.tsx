import React from 'react';
import type { LocalCartItem } from '../state/CartContext';
import { useCart } from '../hooks/useCart';
import { StockStatusBadge } from './StockStatusBadge';
import type { InventoryStatus } from '../types/Product';

export const CartItem: React.FC<{
  item: LocalCartItem;
  inventory?: { status: InventoryStatus; stockQuantity: number };
}> = ({ item, inventory }) => {
  const { updateQuantity, removeItem } = useCart();

  const lineTotal = item.unitPrice * item.quantity;

  return (
    <div className="flex gap-3 border-b border-gray-200 py-3">
      <div className="h-16 w-20 flex-shrink-0 overflow-hidden rounded bg-gray-100">
        {item.imageUrl ? <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" /> : null}
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="truncate text-sm font-medium text-gray-900">{item.productName}</div>
            <div className="mt-1 text-sm text-gray-700">${item.unitPrice.toFixed(2)}</div>
            {inventory ? (
              <div className="mt-2">
                <StockStatusBadge status={inventory.status} stockQuantity={inventory.stockQuantity} />
              </div>
            ) : null}
          </div>
          <div className="text-sm font-medium text-gray-900">${lineTotal.toFixed(2)}</div>
        </div>

        <div className="mt-2 flex items-center gap-2">
          <button
            type="button"
            className="rounded border border-gray-300 px-2 py-1 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={() => updateQuantity(item.productId, item.quantity - 1)}
            aria-label={`Decrease quantity for ${item.productName}`}
          >
            −
          </button>

          <div className="w-10 text-center text-sm text-gray-900" aria-label="Quantity">
            {item.quantity}
          </div>

          <button
            type="button"
            className="rounded border border-gray-300 px-2 py-1 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={() => updateQuantity(item.productId, item.quantity + 1)}
            aria-label={`Increase quantity for ${item.productName}`}
          >
            +
          </button>

          <button
            type="button"
            className="ml-auto text-sm text-gray-700 underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={() => removeItem(item.productId)}
            aria-label={`Remove ${item.productName} from cart`}
          >
            Remove
          </button>
        </div>
      </div>
    </div>
  );
};
