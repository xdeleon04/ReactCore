import React from 'react';
import type { AdminOrderListItem } from '../../types/admin/Order';

export const OrderTable: React.FC<{
  orders: AdminOrderListItem[];
  onViewDetail: (orderId: number) => void;
}> = ({ orders, onViewDetail }) => {
  return (
    <div className="overflow-auto rounded border border-gray-200">
      <table className="w-full border-collapse text-left text-sm" aria-label="Orders table">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50">
            <th className="px-3 py-2 font-medium text-gray-900">Order</th>
            <th className="px-3 py-2 font-medium text-gray-900">Customer</th>
            <th className="px-3 py-2 font-medium text-gray-900">Total</th>
            <th className="px-3 py-2 font-medium text-gray-900">Status</th>
            <th className="px-3 py-2 font-medium text-gray-900">Created</th>
            <th className="px-3 py-2 font-medium text-gray-900">Actions</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((o) => (
            <tr key={o.id} className="border-b border-gray-100" aria-label={`Order ${o.orderNumber}`}>
              <td className="px-3 py-2 text-gray-900">{o.orderNumber}</td>
              <td className="px-3 py-2 text-gray-800">{o.customerEmail}</td>
              <td className="px-3 py-2 text-gray-800">${o.total.toFixed(2)}</td>
              <td className="px-3 py-2 text-gray-800">{o.status}</td>
              <td className="px-3 py-2 text-gray-800">{new Date(o.createdAt).toLocaleString()}</td>
              <td className="px-3 py-2">
                <button
                  type="button"
                  className="rounded border border-gray-300 px-2 py-1 text-sm"
                  onClick={() => onViewDetail(o.id)}
                >
                  View
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
