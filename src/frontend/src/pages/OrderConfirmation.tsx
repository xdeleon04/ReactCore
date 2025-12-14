import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { getOrder } from '../services/orderService';
import type { Order } from '../types/Order';

export const OrderConfirmationPage: React.FC = () => {
  const { orderNumber } = useParams();
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!orderNumber) return;

    void (async () => {
      try {
        setLoading(true);
        const result = await getOrder(orderNumber);
        setOrder(result);
      } catch {
        setError('Unable to load order.');
      } finally {
        setLoading(false);
      }
    })();
  }, [orderNumber]);

  if (loading) {
    return (
      <main className="mx-auto w-full max-w-4xl p-4">
        <div className="text-sm text-gray-700">Loading order…</div>
      </main>
    );
  }

  if (error || !order) {
    return (
      <main className="mx-auto w-full max-w-4xl p-4">
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error ?? 'Order not found.'}</div>
      </main>
    );
  }

  return (
    <main className="mx-auto w-full max-w-4xl p-4">
      <h1 className="text-2xl font-semibold text-gray-900">Order confirmed</h1>
      <p className="mt-1 text-sm text-gray-700">Order number: {order.orderNumber}</p>

      <div className="mt-4 rounded border border-gray-200 bg-white p-4">
        <div className="flex items-center justify-between text-sm">
          <span className="text-gray-700">Status</span>
          <span className="font-medium text-gray-900">{order.status}</span>
        </div>
        <div className="mt-2 flex items-center justify-between text-sm">
          <span className="text-gray-700">Total</span>
          <span className="font-medium text-gray-900">${order.total.toFixed(2)}</span>
        </div>

        <div className="mt-4 border-t border-gray-200 pt-4">
          <h2 className="text-lg font-semibold text-gray-900">Items</h2>
          <div className="mt-3 space-y-3">
            {order.items.map((i) => (
              <div key={i.id} className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="truncate text-sm font-medium text-gray-900">{i.productName}</div>
                  <div className="mt-1 text-sm text-gray-700">Qty {i.quantity}</div>
                </div>
                <div className="text-sm font-medium text-gray-900">${i.lineTotal.toFixed(2)}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </main>
  );
};
