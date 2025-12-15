import React from 'react';
import { Link } from 'react-router-dom';
import type { ProductListItem } from '../types/Product';

function statusLabel(status: ProductListItem['status'], stockQuantity: number): string {
  if (status === 'out-of-stock') return 'Out of Stock';
  if (status === 'low-stock') return `Low Stock (${stockQuantity} left)`;
  return 'In Stock';
}

export const ProductCard: React.FC<{ product: ProductListItem }> = ({ product }) => {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
      <div className="aspect-[4/3] w-full overflow-hidden rounded bg-slate-100">
        <Link
          to={`/products/${product.id}`}
          aria-label={`View details for ${product.name}`}
          className="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50 focus-visible:ring-offset-2"
        >
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="h-full w-full object-cover"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-sm text-slate-700">No image</div>
          )}
        </Link>
      </div>

      <div className="mt-3">
        <h3 className="text-base font-semibold text-slate-900">
          <Link
            to={`/products/${product.id}`}
            className="hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50 focus-visible:ring-offset-2"
          >
            {product.name}
          </Link>
        </h3>
        <p className="mt-1 text-sm text-slate-600">{product.category}</p>
        <div className="mt-2 flex items-center justify-between gap-3">
          <div className="text-sm font-medium text-slate-900">${product.price.toFixed(2)}</div>
          <span
            className="rounded bg-slate-100 px-2 py-1 text-xs text-slate-800"
            aria-label={`Stock status: ${statusLabel(product.status, product.stockQuantity)}`}
          >
            {statusLabel(product.status, product.stockQuantity)}
          </span>
        </div>
      </div>
    </article>
  );
};
