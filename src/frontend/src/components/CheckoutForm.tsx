import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getCart } from '../services/cartService';
import { createOrder, isCreateOrderConflict } from '../services/orderService';
import type { CreateOrderConflictResponse } from '../types/Order';
import { useCart } from '../hooks/useCart';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from './ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from './ui/dialog';

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
    <>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (!canSubmit) return;
          void submit();
        }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Checkout</CardTitle>
          </CardHeader>

          <CardContent className="space-y-4">
            <div className="space-y-1">
              <label htmlFor="checkout-email" className="block text-sm font-medium text-slate-900">
                Email
              </label>
              <Input
                id="checkout-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                maxLength={255}
                autoComplete="email"
                aria-invalid={email.length > 0 && !!emailError}
                aria-describedby={email && emailError ? 'checkout-email-error' : undefined}
              />
              {email && emailError ? (
                <div id="checkout-email-error" className="text-sm text-red-700">
                  {emailError}
                </div>
              ) : null}
            </div>

            <div className="border-t border-slate-200 pt-4">
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-700">Order subtotal</span>
                <span className="font-medium text-slate-900">${subtotal.toFixed(2)}</span>
              </div>
            </div>

            {error ? (
              <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">
                {error}
              </div>
            ) : null}
          </CardContent>

          <CardFooter>
            <Button
              type="submit"
              className="w-full"
              disabled={!canSubmit}
              isLoading={isSubmitting}
              loadingText="Placing order…"
            >
              Place order
            </Button>
          </CardFooter>
        </Card>
      </form>

      <Dialog
        open={!!conflict}
        onOpenChange={(open) => {
          if (!open) setConflict(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Inventory changed</DialogTitle>
            <DialogDescription>Adjust quantities or remove items to continue.</DialogDescription>
          </DialogHeader>

          <div className="px-4 pb-2 sm:px-6">
            {conflict ? (
              <div className="space-y-3">
                {conflict.conflicts.map((c) => (
                  <div key={`${c.productId}-${c.cartItemId}`} className="rounded border border-slate-200 p-3">
                    <div className="text-sm font-medium text-slate-900">{c.productName}</div>
                    <div className="mt-1 text-sm text-slate-700">
                      Requested: {c.requestedQuantity} · Available: {c.availableQuantity}
                    </div>
                    <div className="mt-2 flex flex-wrap items-center gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => updateQuantity(c.productId, Math.max(1, c.availableQuantity))}
                        disabled={c.availableQuantity <= 0}
                      >
                        Set to {Math.max(1, c.availableQuantity)}
                      </Button>
                      <Button type="button" variant="outline" onClick={() => removeItem(c.productId)}>
                        Remove item
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConflict(null)}>
              Continue shopping
            </Button>
            <Button
              type="button"
              onClick={() => {
                setConflict(null);
                void submit();
              }}
            >
              Retry order
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
};
