import { useEffect, useRef } from 'react';
import toast from 'react-hot-toast';
import { useAuth } from '../hooks/useAuth';
import { usePolling } from '../hooks/usePolling';
import { getPendingNotifications } from '../services/inventoryService';

export function NotificationListener() {
  const { isAuthenticated, isLoading } = useAuth();
  const lastToastKeyRef = useRef<string | null>(null);

  useEffect(() => {
    lastToastKeyRef.current = null;
  }, [isAuthenticated]);

  usePolling(
    async () => {
      if (!isAuthenticated || isLoading) return;

      const pending = await getPendingNotifications();
      for (const n of pending) {
        const key = `${n.productId}:${n.triggeredAt}`;
        if (lastToastKeyRef.current === key) continue;
        lastToastKeyRef.current = key;

        toast.success(`${n.productName} is back in stock`);
      }
    },
    5000,
    isAuthenticated && !isLoading
  );

  return null;
}
