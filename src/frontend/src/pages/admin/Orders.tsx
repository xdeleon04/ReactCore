import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { AdminOrderListItem } from '../../types/admin/Order';
import type { PagedResult } from '../../types/admin/AdminResponse';
import { listOrders } from '../../services/admin/adminOrderService';
import { EmptyState } from '../../components/EmptyState';
import { OrderTable } from '../../components/admin/OrderTable';
import { OrderDetailModal } from '../../components/admin/OrderDetailModal';

const DEFAULT_TAKE = 20;

export const OrdersPage: React.FC = () => {
  const [orderNumber, setOrderNumber] = useState('');
  const [email, setEmail] = useState('');
  const [status, setStatus] = useState('');

  const [skip, setSkip] = useState(0);
  const [take, setTake] = useState(DEFAULT_TAKE);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<PagedResult<AdminOrderListItem> | null>(null);

  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);

  const statusParam = useMemo(() => (status.trim() ? status.trim() : undefined), [status]);

  const fetchOrders = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await listOrders({
        orderNumber: orderNumber.trim() || undefined,
        email: email.trim() || undefined,
        status: statusParam,
        skip,
        take,
      });
      setData(result);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to load orders.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [email, orderNumber, skip, statusParam, take]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  const onSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSkip(0);
    fetchOrders();
  };

  const pageStart = data?.skip ?? skip;
  const pageSize = data?.take ?? take;
  const total = data?.total ?? 0;
  const canPrev = pageStart > 0;
  const canNext = pageStart + pageSize < total;

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · Orders</h1>
        <p className="mt-1 text-sm text-gray-700">Search orders, review details, and update statuses.</p>
      </header>

      <form className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-4" onSubmit={onSearchSubmit} aria-label="Order filters">
        <div className="flex flex-col gap-1">
          <label htmlFor="orderNumber" className="text-sm font-medium text-gray-900">
            Order #
          </label>
          <input
            id="orderNumber"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={orderNumber}
            onChange={(e) => setOrderNumber(e.target.value)}
            placeholder="Search by order number"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="email" className="text-sm font-medium text-gray-900">
            Customer email
          </label>
          <input
            id="email"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Search by email"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="status" className="text-sm font-medium text-gray-900">
            Status
          </label>
          <select
            id="status"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="Shipped">Shipped</option>
            <option value="Completed">Completed</option>
          </select>
        </div>

        <div className="flex items-end">
          <button
            type="submit"
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
            disabled={loading}
          >
            {loading ? 'Searching…' : 'Search'}
          </button>
        </div>
      </form>

      {loading ? <div className="text-sm text-gray-700">Loading orders…</div> : null}
      {error ? (
        <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      ) : null}

      {!loading && data && data.items.length === 0 ? (
        <EmptyState title="No orders found" description="Try adjusting your search and filters." />
      ) : null}

      {data && data.items.length > 0 ? (
        <div className="space-y-4">
          <OrderTable orders={data.items} onViewDetail={setSelectedOrderId} />

          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm text-gray-700">
              Showing {Math.min(total, pageStart + 1)}–{Math.min(total, pageStart + pageSize)} of {total}
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                disabled={!canPrev || loading}
                onClick={() => setSkip((prev) => Math.max(0, prev - pageSize))}
              >
                Prev
              </button>
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                disabled={!canNext || loading}
                onClick={() => setSkip((prev) => prev + pageSize)}
              >
                Next
              </button>
              <select
                aria-label="Page size"
                className="rounded border border-gray-300 px-3 py-2 text-sm"
                value={take}
                onChange={(e) => {
                  const nextTake = Number(e.target.value) || DEFAULT_TAKE;
                  setTake(nextTake);
                  setSkip(0);
                }}
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>
        </div>
      ) : null}

      {selectedOrderId ? (
        <OrderDetailModal
          orderId={selectedOrderId}
          open={true}
          onClose={() => setSelectedOrderId(null)}
          onUpdated={fetchOrders}
        />
      ) : null}
    </main>
  );
};
