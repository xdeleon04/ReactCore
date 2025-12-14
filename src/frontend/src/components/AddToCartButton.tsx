import React, { useMemo, useState } from 'react';
import type { ProductDetail, ProductListItem } from '../types/Product';
import { useCart } from '../hooks/useCart';

type AddableProduct = Pick<ProductDetail, 'id' | 'name' | 'price' | 'imageUrl' | 'status'> | Pick<ProductListItem, 'id' | 'name' | 'price' | 'imageUrl' | 'status'>;

export const AddToCartButton: React.FC<{ product: AddableProduct }> = ({ product }) => {
  const { addItem } = useCart();
  const [quantity, setQuantity] = useState(1);

  const quantityInputId = useMemo(() => `qty-${product.id}`, [product.id]);

  const disabled = product.status === 'out-of-stock';
  const max = useMemo(() => 99, []);

  return (
    <div className="flex items-center gap-3">
      <label htmlFor={quantityInputId} className="flex items-center gap-2 text-sm text-gray-700">
        Qty
        <input
          id={quantityInputId}
          type="number"
          min={1}
          max={max}
          value={quantity}
          onChange={(e) => setQuantity(Math.max(1, Math.min(max, Number(e.target.value) || 1)))}
          className="w-20 rounded border border-gray-300 px-2 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
          disabled={disabled}
        />
      </label>

      <button
        type="button"
        className="rounded bg-gray-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
        disabled={disabled}
        onClick={() =>
          addItem(
            {
              productId: product.id,
              productName: product.name,
              unitPrice: product.price,
              imageUrl: product.imageUrl ?? null,
            },
            quantity
          )
        }
      >
        Add to cart
      </button>
    </div>
  );
};
