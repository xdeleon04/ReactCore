import { useEffect, useRef } from "react";

export function usePolling(fn: () => void | Promise<void>, intervalMs: number, enabled: boolean) {
  const fnRef = useRef(fn);

  useEffect(() => {
    fnRef.current = fn;
  }, [fn]);

  useEffect(() => {
    if (!enabled) return;

    let cancelled = false;

    const tick = async () => {
      try {
        await fnRef.current();
      } catch {
        // Let caller handle errors inside fn
      }
    };

    void tick();

    const id = window.setInterval(() => {
      if (cancelled) return;
      void tick();
    }, intervalMs);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [enabled, intervalMs]);
}
