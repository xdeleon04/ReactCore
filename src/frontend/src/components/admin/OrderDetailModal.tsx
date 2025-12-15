import React, { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import type { AdminOrderDetail } from '../../types/admin/Order';
import { getOrderDetail, updateOrderStatus } from '../../services/admin/adminOrderService';

function getApiErrorMessage(error: any, fallback: string): string {
  return (
    error?.response?.data?.message ||
    error?.response?.data?.error ||
    error?.message ||
    fallback
  );
}

export const OrderDetailModal: React.FC<{
  orderId: number;
  open: boolean;
  onClose: () => void;
  onUpdated?: () => void;
}> = ({ orderId, open, onClose, onUpdated }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<AdminOrderDetail | null>(null);

  const [nextStatus, setNextStatus] = useState('');
  const [reason, setReason] = useState('');
  const [updating, setUpdating] = useState(false);
  const [updateError, setUpdateError] = useState<string | null>(null);

  const allowedTransitions = useMemo(() => detail?.allowedStatusTransitions ?? [], [detail]);

  const load = async () => {
    setLoading(true);
    setError(null);

    try {
      const d = await getOrderDetail(orderId);
      setDetail(d);
      setNextStatus(d.allowedStatusTransitions[0] ?? '');
    } catch (e: any) {
      setError(getApiErrorMessage(e, 'Unable to load order details.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!open) return;

    let cancelled = false;
    setUpdateError(null);

    setLoading(true);
    setError(null);

    getOrderDetail(orderId)
      .then((d) => {
        if (cancelled) return;
        setDetail(d);
        setNextStatus(d.allowedStatusTransitions[0] ?? '');
      })
      .catch((e) => {
        if (cancelled) return;
        setError(getApiErrorMessage(e, 'Unable to load order details.'));
      })
      .finally(() => {
        if (cancelled) return;
        setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [open, orderId]);

  if (!open) return null;

  const canUpdate = allowedTransitions.length > 0 && nextStatus.trim().length > 0;

  const handleUpdate = async () => {
    if (!canUpdate) return;

    setUpdating(true);
    setUpdateError(null);

    try {
      await updateOrderStatus(orderId, {
        status: nextStatus,
        reason: reason.trim() || undefined,
      });

      toast.success('Order status updated');
      await load();
      onUpdated?.();
    } catch (e: any) {
      setUpdateError(getApiErrorMessage(e, 'Unable to update order status.'));
    } finally {
      setUpdating(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Order details">
      <button type="button" className="absolute inset-0 bg-black/40" onClick={onClose} aria-label="Close order details" />

      <div className="absolute left-1/2 top-1/2 w-[min(840px,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded bg-white p-4 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Order detail</h2>
            <div className="mt-1 text-sm text-gray-700">Order #{orderId}</div>
          </div>
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={onClose}
          >
            Close
          </button>
        </div>

        <div className="mt-4">
          {loading ? <div className="text-sm text-gray-700">Loading order…</div> : null}
          {error ? (
            <div role="alert" className="mb-3 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
              {error}
            </div>
          ) : null}

          {detail ? (
            <div className="space-y-4">
              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Summary</div>
                <div className="mt-2 grid grid-cols-1 gap-2 text-sm text-gray-800 sm:grid-cols-2">
                  <div>
                    <span className="font-medium">Order:</span> {detail.orderNumber}
                  </div>
                  <div>
                    <span className="font-medium">Customer:</span> {detail.customerEmail}
                  </div>
                  <div>
                    <span className="font-medium">Status:</span> {detail.status}
                  </div>
                  <div>
                    <span className="font-medium">Created:</span> {new Date(detail.createdAt).toLocaleString()}
                  </div>
                </div>
              </section>

              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Items</div>
                <div className="mt-2 overflow-auto">
                  <table className="w-full border-collapse text-left text-sm">
                    <thead>
                      <tr className="border-b border-gray-200 bg-gray-50">
                        <th className="px-3 py-2 font-medium text-gray-900">Product</th>
                        <th className="px-3 py-2 font-medium text-gray-900">Qty</th>
                        <th className="px-3 py-2 font-medium text-gray-900">Unit</th>
                        <th className="px-3 py-2 font-medium text-gray-900">Line</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.items.map((i) => (
                        <tr key={`${i.productId}-${i.productName}`} className="border-b border-gray-100">
                          <td className="px-3 py-2 text-gray-900">{i.productName}</td>
                          <td className="px-3 py-2 text-gray-800">{i.quantity}</td>
                          <td className="px-3 py-2 text-gray-800">${i.unitPrice.toFixed(2)}</td>
                          <td className="px-3 py-2 text-gray-800">${i.lineTotal.toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="mt-3 flex flex-wrap items-center justify-end gap-4 text-sm text-gray-800">
                  <div>
                    <span className="font-medium">Subtotal:</span> ${detail.subtotal.toFixed(2)}
                  </div>
                  <div>
                    <span className="font-medium">Total:</span> ${detail.total.toFixed(2)}
                  </div>
                </div>
              </section>

              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Update status</div>

                {updateError ? (
                  <div role="alert" className="mt-2 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
                    {updateError}
                  </div>
                ) : null}

                <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-3">
                  <div className="flex flex-col gap-1">
                    <label htmlFor="nextStatus" className="text-sm font-medium text-gray-900">
                      Next status
                    </label>
                    <select
                      id="nextStatus"
                      className="rounded border border-gray-300 px-3 py-2 text-sm"
                      value={nextStatus}
                      onChange={(e) => setNextStatus(e.target.value)}
                      disabled={allowedTransitions.length === 0 || updating}
                    >
                      {allowedTransitions.length === 0 ? <option value="">No transitions available</option> : null}
                      {allowedTransitions.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="sm:col-span-2 flex flex-col gap-1">
                    <label htmlFor="reason" className="text-sm font-medium text-gray-900">
                      Reason (optional)
                    </label>
                    <input
                      id="reason"
                      className="rounded border border-gray-300 px-3 py-2 text-sm"
                      value={reason}
                      onChange={(e) => setReason(e.target.value)}
                      placeholder="Why are you changing the status?"
                      disabled={updating}
                      maxLength={500}
                    />
                  </div>
                </div>

                <div className="mt-3 flex justify-end">
                  <button
                    type="button"
                    className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                    onClick={handleUpdate}
                    disabled={!canUpdate || updating}
                  >
                    {updating ? 'Updating…' : 'Update status'}
                  </button>
                </div>
              </section>
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
};
