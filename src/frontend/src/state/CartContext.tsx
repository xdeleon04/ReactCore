import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useLocalStorage } from "../hooks/useLocalStorage";
import { useAuth } from "../hooks/useAuth";
import { addToCart, getCart, removeCartItem, updateCartItem } from "../services/cartService";
import toast from "react-hot-toast";

export interface LocalCartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  imageUrl?: string | null;
  quantity: number;
}

interface CartContextType {
  items: LocalCartItem[];
  itemCount: number;
  subtotal: number;
  total: number;
  loading: boolean;
  refresh: () => Promise<void>;
  addItem: (item: Omit<LocalCartItem, "quantity">, quantity: number) => void;
  updateQuantity: (productId: number, quantity: number) => void;
  removeItem: (productId: number) => void;
  clear: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const CartContext = createContext<CartContextType | undefined>(undefined);

const STORAGE_KEY = "anon_cart_v1";
const EMPTY_LOCAL_CART: LocalCartItem[] = [];

export function CartProvider({ children }: { children: ReactNode }) {
  const { value: items, setValue: setItems, remove } = useLocalStorage<LocalCartItem[]>(STORAGE_KEY, EMPTY_LOCAL_CART);
  const { isAuthenticated, user, isLoading: authLoading } = useAuth();

  const [serverItems, setServerItems] = useState<LocalCartItem[] | null>(null);
  const isSyncingRef = useRef(false);
  const isRefreshingRef = useRef(false);
  const lastMergedUserKeyRef = useRef<string | null>(null);

  const effectiveItems = useMemo(() => (isAuthenticated ? (serverItems ?? []) : items), [isAuthenticated, items, serverItems]);
  const loading = (isAuthenticated && serverItems === null) || (authLoading && items.length === 0);

  const addItem = useCallback(
    (item: Omit<LocalCartItem, "quantity">, quantity: number) => {
      if (quantity <= 0) return;

      if (isAuthenticated) {
        const previous = serverItems ?? [];
        const optimistic = (() => {
          const existing = previous.find((x) => x.productId === item.productId);
          if (!existing) return [...previous, { ...item, quantity }];
          return previous.map((x) =>
            x.productId === item.productId ? { ...x, quantity: x.quantity + quantity } : x
          );
        })();
        setServerItems(optimistic);
        toast.success("Added to cart.");

        void (async () => {
          try {
            await addToCart({ productId: item.productId, quantity });
            const cart = await getCart();
            setServerItems(
              cart.items.map((ci) => ({
                productId: ci.productId,
                productName: ci.productName,
                unitPrice: ci.unitPrice,
                imageUrl: ci.imageUrl ?? null,
                quantity: ci.quantity,
              }))
            );
          } catch (e) {
            setServerItems(previous);
            toast.error("Unable to add to cart. Please try again.");
            console.error(e);
          }
        })();
        return;
      }

      toast.success("Added to cart.");
      setItems((prev) => {
        const existing = prev.find((x) => x.productId === item.productId);
        if (!existing) {
          return [...prev, { ...item, quantity }];
        }
        return prev.map((x) => (x.productId === item.productId ? { ...x, quantity: x.quantity + quantity } : x));
      });
    },
    [isAuthenticated, serverItems, setItems]
  );

  const refresh = useCallback(async () => {
    if (!isAuthenticated) return;
    if (isSyncingRef.current) return;
    if (isRefreshingRef.current) return;

    isRefreshingRef.current = true;
    try {
      const cart = await getCart();
      setServerItems(
        cart.items.map((ci) => ({
          productId: ci.productId,
          productName: ci.productName,
          unitPrice: ci.unitPrice,
          imageUrl: ci.imageUrl ?? null,
          quantity: ci.quantity,
        }))
      );
    } catch (e) {
      console.error(e);
    } finally {
      isRefreshingRef.current = false;
    }
  }, [isAuthenticated]);

  const updateQuantity = useCallback(
    (productId: number, quantity: number) => {
      if (isAuthenticated) {
        const previous = serverItems ?? [];
        const optimistic = quantity <= 0
          ? previous.filter((x) => x.productId !== productId)
          : previous.map((x) => (x.productId === productId ? { ...x, quantity } : x));
        setServerItems(optimistic);
        toast.success("Cart updated.");

        void (async () => {
          try {
            const cart = await getCart();
            const match = cart.items.find((ci) => ci.productId === productId);
            if (!match) return;

            if (quantity <= 0) {
              await removeCartItem(match.id);
            } else {
              await updateCartItem(match.id, { quantity });
            }

            const refreshed = await getCart();
            setServerItems(
              refreshed.items.map((ci) => ({
                productId: ci.productId,
                productName: ci.productName,
                unitPrice: ci.unitPrice,
                imageUrl: ci.imageUrl ?? null,
                quantity: ci.quantity,
              }))
            );
          } catch (e) {
            setServerItems(previous);
            toast.error("Unable to update cart. Please try again.");
            console.error(e);
          }
        })();
        return;
      }

      if (quantity <= 0) {
        toast.success("Removed from cart.");
      } else {
        toast.success("Cart updated.");
      }
      setItems((prev) => {
        if (quantity <= 0) return prev.filter((x) => x.productId !== productId);
        return prev.map((x) => (x.productId === productId ? { ...x, quantity } : x));
      });
    },
    [isAuthenticated, serverItems, setItems]
  );

  const removeItem = useCallback(
    (productId: number) => {
      if (isAuthenticated) {
        const previous = serverItems ?? [];
        setServerItems(previous.filter((x) => x.productId !== productId));
        toast.success("Removed from cart.");

        void (async () => {
          try {
            const cart = await getCart();
            const match = cart.items.find((ci) => ci.productId === productId);
            if (!match) return;
            await removeCartItem(match.id);
            const refreshed = await getCart();
            setServerItems(
              refreshed.items.map((ci) => ({
                productId: ci.productId,
                productName: ci.productName,
                unitPrice: ci.unitPrice,
                imageUrl: ci.imageUrl ?? null,
                quantity: ci.quantity,
              }))
            );
          } catch (e) {
            setServerItems(previous);
            toast.error("Unable to remove item. Please try again.");
            console.error(e);
          }
        })();
        return;
      }

      toast.success("Removed from cart.");
      setItems((prev) => prev.filter((x) => x.productId !== productId));
    },
    [isAuthenticated, serverItems, setItems]
  );

  const clear = useCallback(() => {
    if (isAuthenticated) {
      const previous = serverItems ?? [];
      setServerItems([]);
      toast.success("Cart cleared.");

      void (async () => {
        try {
          const cart = await getCart();
          await Promise.all(cart.items.map((ci) => removeCartItem(ci.id)));
          const refreshed = await getCart();
          setServerItems(
            refreshed.items.map((ci) => ({
              productId: ci.productId,
              productName: ci.productName,
              unitPrice: ci.unitPrice,
              imageUrl: ci.imageUrl ?? null,
              quantity: ci.quantity,
            }))
          );
        } catch (e) {
          setServerItems(previous);
          toast.error("Unable to clear cart. Please try again.");
          console.error(e);
        }
      })();
      return;
    }

    toast.success("Cart cleared.");
    remove();
  }, [isAuthenticated, remove, serverItems]);

  useEffect(() => {
    if (!isAuthenticated) {
      setServerItems(null);
      lastMergedUserKeyRef.current = null;
      return;
    }

    if (isSyncingRef.current) return;
    // Avoid kicking off cart sync while auth is still initializing.
    if (authLoading) return;

    const userKey = (user?.id || user?.email || '__unknown_user__').trim();

    void (async () => {
      isSyncingRef.current = true;
      try {
        if (items.length > 0 && lastMergedUserKeyRef.current !== userKey) {
          for (const item of items) {
            await addToCart({ productId: item.productId, quantity: item.quantity });
          }
          remove();
          lastMergedUserKeyRef.current = userKey;
        }

        const cart = await getCart();
        setServerItems(
          cart.items.map((ci) => ({
            productId: ci.productId,
            productName: ci.productName,
            unitPrice: ci.unitPrice,
            imageUrl: ci.imageUrl ?? null,
            quantity: ci.quantity,
          }))
        );
      } catch (e) {
        console.error(e);
      } finally {
        isSyncingRef.current = false;
      }
    })();
  }, [authLoading, isAuthenticated, items, remove, user?.email, user?.id]);

  const derived = useMemo(() => {
    const itemCount = effectiveItems.reduce((sum, item) => sum + item.quantity, 0);
    const subtotal = effectiveItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
    return { itemCount, subtotal, total: subtotal };
  }, [effectiveItems]);

  const value = useMemo<CartContextType>(
    () => ({
      items: effectiveItems,
      itemCount: derived.itemCount,
      subtotal: derived.subtotal,
      total: derived.total,
      loading,
      refresh,
      addItem,
      updateQuantity,
      removeItem,
      clear,
    }),
    [addItem, clear, derived.itemCount, derived.subtotal, derived.total, effectiveItems, loading, refresh, removeItem, updateQuantity]
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}
