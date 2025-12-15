import React from 'react';
import type { AdminProductListItem } from '../../types/admin/Product';

type Props = {
  products: AdminProductListItem[];
  busyProductId?: number | null;
  onEdit: (productId: number) => void;
  onArchive: (productId: number) => void;
};

function getStockBadgeText(p: AdminProductListItem): string {
  if (p.isDeleted) return 'Archived';
  if (p.stockQuantity <= p.reorderLevel) return 'Low stock';
  return 'OK';
}

function getStockBadgeClasses(p: AdminProductListItem): string {
  if (p.isDeleted) return 'bg-gray-100 text-gray-800 border-gray-200';
  if (p.stockQuantity <= p.reorderLevel) return 'bg-yellow-50 text-yellow-900 border-yellow-200';
  return 'bg-green-50 text-green-900 border-green-200';
}

export const ProductTable: React.FC<Props> = ({ products, busyProductId, onEdit, onArchive }) => {
  return (
    <div className="overflow-x-auto rounded border border-gray-200">
      <table className="min-w-full divide-y divide-gray-200" aria-label="Products table">
        <thead className="bg-gray-50">
          <tr>
            <th scope="col" className="px-4 py-3 text-left text-xs font-medium text-gray-700">
              Name
            </th>
            <th scope="col" className="px-4 py-3 text-left text-xs font-medium text-gray-700">
              Category
            </th>
            <th scope="col" className="px-4 py-3 text-right text-xs font-medium text-gray-700">
              Price
            </th>
            <th scope="col" className="px-4 py-3 text-right text-xs font-medium text-gray-700">
              Stock
            </th>
            <th scope="col" className="px-4 py-3 text-left text-xs font-medium text-gray-700">
              Status
            </th>
            <th scope="col" className="px-4 py-3 text-right text-xs font-medium text-gray-700">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-200 bg-white">
          {products.map((p) => {
            const busy = busyProductId === p.id;
            const badgeText = getStockBadgeText(p);

            return (
              <tr key={p.id} className="hover:bg-gray-50">
                <td className="whitespace-nowrap px-4 py-3 text-sm font-medium text-gray-900">{p.name}</td>
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-700">{p.category}</td>
                <td className="whitespace-nowrap px-4 py-3 text-right text-sm text-gray-700">${p.price.toFixed(2)}</td>
                <td className="whitespace-nowrap px-4 py-3 text-right text-sm text-gray-700">{p.stockQuantity}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <span className={`inline-flex items-center rounded border px-2 py-0.5 text-xs ${getStockBadgeClasses(p)}`}>
                    {badgeText}
                  </span>
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-right">
                  <div className="flex justify-end gap-2">
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-1.5 text-sm disabled:opacity-50"
                      onClick={() => onEdit(p.id)}
                      disabled={busy}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-1.5 text-sm disabled:opacity-50"
                      onClick={() => onArchive(p.id)}
                      disabled={busy || p.isDeleted}
                      aria-label={`Archive product ${p.name}`}
                    >
                      {p.isDeleted ? 'Archived' : 'Archive'}
                    </button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
