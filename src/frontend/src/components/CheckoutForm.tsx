import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getCart } from '../services/cartService';
import { createOrder, isCreateOrderConflict } from '../services/orderService';
import type { CreateOrderConflictResponse } from '../types/Order';
import { useCart } from '../hooks/useCart';

function validateEmail(email: string): string | null {
  const trimmed = email.trim();
  if (!trimmed) return 'Email is required.';
  if (trimmed.length > 255) return 'Email must be 255 characters or less.';
  // Simple sanity check; backend performs authoritative validation.
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)) return 'Email must be in a valid format (example@domain.com).';
  return null;
}

export const CheckoutForm: React.FC = () => {
  const navigate = useNavigate();
  const { items, subtotal, clear, updateQuantity, removeItem } = useCart();

  const [email, setEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [conflict, setConflict] = useState<CreateOrderConflictResponse | null>(null);

  const emailError = useMemo(() => validateEmail(email), [email]);
  const canSubmit = items.length > 0 && !emailError && !isSubmitting;

  const submit = async () => {
    setError(null);
    setConflict(null);

    const validation = validateEmail(email);
    if (validation) {
      setError(validation);
      return;
    }

    setIsSubmitting(true);
    try {
      const cart = await getCart();
      const order = await createOrder({ email: email.trim(), cartId: cart.id });
      clear();
      navigate(`/orders/${encodeURIComponent(order.orderNumber)}`);
    } catch (e) {
      if (isCreateOrderConflict(e)) {
        setConflict(e.response.data);
      } else {
        setError('Unable to place order. Please try again.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form
      className="rounded border border-gray-200 bg-white p-4"
      onSubmit={(e) => {
        e.preventDefault();
        if (!canSubmit) return;
        void submit();
      }}
    >
      <h2 className="text-lg font-semibold text-gray-900">Checkout</h2>

      <div className="mt-3">
        <label htmlFor="checkout-email" className="block text-sm font-medium text-gray-900">
          Email
        </label>
        <input
          id="checkout-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="mt-1 w-full rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
          maxLength={255}
          autoComplete="email"
          aria-invalid={email.length > 0 && !!emailError}
          aria-describedby={email && emailError ? 'checkout-email-error' : undefined}
        />
        {email && emailError ? (
          <div id="checkout-email-error" className="mt-1 text-sm text-red-700">
            {emailError}
          </div>
        ) : null}
      </div>

      <div className="mt-4 border-t border-gray-200 pt-4">
        <div className="flex items-center justify-between text-sm">
          <span className="text-gray-700">Order subtotal</span>
          <span className="font-medium text-gray-900">${subtotal.toFixed(2)}</span>
        </div>
      </div>

      {error ? <div className="mt-3 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</div> : null}

      <button
        type="submit"
        className="mt-4 w-full rounded bg-gray-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
        disabled={!canSubmit}
      >
        {isSubmitting ? 'Placing order…' : 'Place order'}
      </button>

      {conflict ? (
        <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Inventory conflict">
          <button
            type="button"
            className="absolute inset-0 bg-black/40"
            onClick={() => setConflict(null)}
            aria-label="Close conflict dialog"
          />
          <div className="absolute left-1/2 top-1/2 w-[min(720px,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded bg-white p-4 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h3 className="text-lg font-semibold text-gray-900">Inventory changed</h3>
                <p className="mt-1 text-sm text-gray-700">Adjust quantities or remove items to continue.</p>
              </div>
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                onClick={() => setConflict(null)}
              >
                Close
              </button>
            </div>

            <div className="mt-4 space-y-3">
              {conflict.conflicts.map((c) => (
                <div key={`${c.productId}-${c.cartItemId}`} className="rounded border border-gray-200 p-3">
                  <div className="text-sm font-medium text-gray-900">{c.productName}</div>
                  <div className="mt-1 text-sm text-gray-700">
                    Requested: {c.requestedQuantity} · Available: {c.availableQuantity}
                  </div>
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                      onClick={() => updateQuantity(c.productId, Math.max(1, c.availableQuantity))}
                      disabled={c.availableQuantity <= 0}
                    >
                      Set to {Math.max(1, c.availableQuantity)}
                    </button>
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                      onClick={() => removeItem(c.productId)}
                    >
                      Remove item
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="mt-4 flex items-center justify-end gap-2">
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                onClick={() => setConflict(null)}
              >
                Continue shopping
              </button>
              <button
                type="button"
                className="rounded bg-gray-900 px-4 py-2 text-sm font-medium text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                onClick={() => {
                  setConflict(null);
                  void submit();
                }}
              >
                Retry order
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </form>
  );
};
