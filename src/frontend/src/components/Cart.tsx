import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../hooks/useCart';
import { CartItem } from './CartItem';
import { usePolling } from '../hooks/usePolling';
import { getInventoryStatus } from '../services/inventoryService';
import type { InventoryStatus } from '../types/Product';
import { EmptyState } from './EmptyState';
import { CartSkeleton } from './skeletons/CartSkeleton';

export const Cart: React.FC<{ open: boolean; onClose: () => void }> = ({ open, onClose }) => {
  const { items, subtotal, itemCount, clear, loading } = useCart();
  const navigate = useNavigate();

  const [inventoryByProductId, setInventoryByProductId] = React.useState<
    Record<number, { status: InventoryStatus; stockQuantity: number }>
  >({});

  usePolling(
    async () => {
      if (!open) return;
      if (items.length === 0) return;

      const productIds = Array.from(new Set(items.map((i) => i.productId)));
      try {
        const results = await Promise.all(
          productIds.map(async (productId) => {
            const inv = await getInventoryStatus(productId);
            return {
              productId,
              status: inv.status as InventoryStatus,
              stockQuantity: inv.stockQuantity,
            };
          })
        );

        setInventoryByProductId((prev) => {
          const next = { ...prev };
          for (const r of results) {
            next[r.productId] = { status: r.status, stockQuantity: r.stockQuantity };
          }
          return next;
        });
      } catch {
        // Ignore polling errors
      }
    },
    5000,
    open
  );

  React.useEffect(() => {
    if (!open) return;

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onClose, open]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Shopping cart">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-label="Close cart"
      />

      <aside className="absolute right-0 top-0 h-full w-full max-w-md bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-gray-200 p-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Cart</h2>
            <div className="text-sm text-gray-700">{itemCount} items</div>
          </div>
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={onClose}
          >
            Close
          </button>
        </div>

        <div className="h-[calc(100%-160px)] overflow-auto p-4">
          {loading ? (
            <CartSkeleton />
          ) : items.length === 0 ? (
            <EmptyState title="Your cart is empty" description="Add products from the shop to get started." />
          ) : (
            items.map((i) => <CartItem key={i.productId} item={i} inventory={inventoryByProductId[i.productId]} />)
          )}
        </div>

        <div className="border-t border-gray-200 p-4">
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-700">Subtotal</span>
            <span className="font-medium text-gray-900">${subtotal.toFixed(2)}</span>
          </div>
          <div className="mt-3 flex items-center gap-2">
            <button
              type="button"
              className="flex-1 rounded bg-gray-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
              disabled={items.length === 0}
              onClick={() => {
                onClose();
                navigate('/checkout');
              }}
            >
              Checkout
            </button>
            <button
              type="button"
              className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
              onClick={clear}
              disabled={items.length === 0}
            >
              Clear
            </button>
          </div>
        </div>
      </aside>
    </div>
  );
};
