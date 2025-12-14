import React, { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import type { InventoryStatus } from '../types/Product';
import { useAuth } from '../hooks/useAuth';
import { useLocalStorage } from '../hooks/useLocalStorage';
import { subscribeToRestock } from '../services/inventoryService';

type LocalNotifyPreference = {
  productId: number;
  expiresAt: number; // epoch ms
};

const STORAGE_KEY = 'anon_notify_v1';
const EXPIRY_MS = 24 * 60 * 60 * 1000;

export const NotifyMeButton: React.FC<{ productId: number; status: InventoryStatus }> = ({
  productId,
  status,
}) => {
  const { isAuthenticated } = useAuth();
  const { value: prefs, setValue: setPrefs } = useLocalStorage<LocalNotifyPreference[]>(STORAGE_KEY, []);

  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const now = Date.now();
    const next = prefs.filter((p) => p.expiresAt > now);
    if (next.length !== prefs.length) {
      setPrefs(next);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const isOutOfStock = status === 'out-of-stock';

  const isSubscribed = useMemo(() => {
    const now = Date.now();
    return prefs.some((p) => p.productId === productId && p.expiresAt > now);
  }, [prefs, productId]);

  const disabled = !isOutOfStock || isSubmitting || isSubscribed;

  const onClick = async () => {
    if (disabled) return;

    setIsSubmitting(true);
    try {
      if (isAuthenticated) {
        await subscribeToRestock(productId);
        toast.success('Subscribed to restock notifications');
      } else {
        const now = Date.now();
        const expiresAt = now + EXPIRY_MS;

        setPrefs((prev) => {
          const filtered = prev.filter((p) => !(p.productId === productId && p.expiresAt > now));
          return [...filtered, { productId, expiresAt }];
        });

        toast.success('Saved notification preference (24 hours)');
      }
    } catch {
      toast.error('Unable to subscribe. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <button
      type="button"
      className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
      disabled={disabled}
      onClick={onClick}
      aria-label="Notify me when back in stock"
    >
      {isSubscribed ? 'Subscribed' : isSubmitting ? 'Subscribing…' : 'Notify me'}
    </button>
  );
};
