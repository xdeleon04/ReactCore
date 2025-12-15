import React from 'react';
import { CheckoutForm } from '../components/CheckoutForm';
import { useCart } from '../hooks/useCart';
import { EmptyState } from '../components/EmptyState';
import { useNavigate } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';

export const CheckoutPage: React.FC = () => {
  const { items, subtotal, loading } = useCart();
  const navigate = useNavigate();

  return (
    <main className="space-y-4">
      <header>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Checkout</h1>
      </header>

      <div className="grid gap-4 lg:grid-cols-3">
        <section className="lg:col-span-2">
          <CheckoutForm />
        </section>

        <section className="lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle>Order summary</CardTitle>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="text-sm text-slate-700">Loading your cart…</div>
              ) : items.length === 0 ? (
                <EmptyState
                  title="Your cart is empty"
                  description="Add products from the shop to place an order."
                  actionLabel="Go to shop"
                  onAction={() => navigate('/shop')}
                />
              ) : (
                <div className="space-y-3">
                  {items.map((i) => (
                    <div key={i.productId} className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <div className="truncate text-sm font-medium text-slate-900">{i.productName}</div>
                        <div className="mt-1 text-sm text-slate-700">Qty {i.quantity}</div>
                      </div>
                      <div className="text-sm font-medium text-slate-900">${(i.unitPrice * i.quantity).toFixed(2)}</div>
                    </div>
                  ))}
                  <div className="border-t border-slate-200 pt-3 text-sm">
                    <div className="flex items-center justify-between">
                      <span className="text-slate-700">Subtotal</span>
                      <span className="font-medium text-slate-900">${subtotal.toFixed(2)}</span>
                    </div>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </section>
      </div>
    </main>
  );
};
