import React from 'react';
import { CheckoutForm } from '../components/CheckoutForm';
import { useCart } from '../hooks/useCart';
import { EmptyState } from '../components/EmptyState';
import { useNavigate } from 'react-router-dom';

export const CheckoutPage: React.FC = () => {
  const { items, subtotal, loading } = useCart();
  const navigate = useNavigate();

  return (
    <main className="mx-auto w-full max-w-4xl p-4">
      <h1 className="text-2xl font-semibold text-gray-900">Checkout</h1>

      <div className="mt-4 grid gap-4 md:grid-cols-2">
        <section className="rounded border border-gray-200 bg-white p-4">
          <h2 className="text-lg font-semibold text-gray-900">Order summary</h2>
          {loading ? (
            <div className="mt-3 text-sm text-gray-700">Loading your cart…</div>
          ) : items.length === 0 ? (
            <div className="mt-3">
              <EmptyState
                title="Your cart is empty"
                description="Add products from the shop to place an order."
                actionLabel="Go to shop"
                onAction={() => navigate('/shop')}
              />
            </div>
          ) : (
            <div className="mt-3 space-y-3">
              {items.map((i) => (
                <div key={i.productId} className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <div className="truncate text-sm font-medium text-gray-900">{i.productName}</div>
                    <div className="mt-1 text-sm text-gray-700">Qty {i.quantity}</div>
                  </div>
                  <div className="text-sm font-medium text-gray-900">${(i.unitPrice * i.quantity).toFixed(2)}</div>
                </div>
              ))}
              <div className="border-t border-gray-200 pt-3 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-gray-700">Subtotal</span>
                  <span className="font-medium text-gray-900">${subtotal.toFixed(2)}</span>
                </div>
              </div>
            </div>
          )}
        </section>

        <section>
          <CheckoutForm />
        </section>
      </div>
    </main>
  );
};
