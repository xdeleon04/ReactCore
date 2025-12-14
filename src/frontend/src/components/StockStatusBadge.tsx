import React from 'react';
import type { InventoryStatus } from '../types/Product';

export const StockStatusBadge: React.FC<{
  status: InventoryStatus;
  stockQuantity: number;
}> = ({ status, stockQuantity }) => {
  const text =
    status === 'out-of-stock'
      ? 'Out of Stock'
      : status === 'low-stock'
        ? `Low Stock (${stockQuantity} left)`
        : 'In Stock';

  return <span className="rounded bg-gray-100 px-2 py-1 text-xs text-gray-800">{text}</span>;
};
