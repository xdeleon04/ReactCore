import React from 'react';
import { Link } from 'react-router-dom';
import type { RelatedProduct } from '../types/Product';

export const RelatedProducts: React.FC<{ products: RelatedProduct[] }> = ({ products }) => {
  if (products.length === 0) {
    return <div className="text-sm text-gray-700">No related products.</div>;
  }

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {products.map((p) => (
        <Link
          key={p.id}
          to={`/products/${p.id}`}
          className="rounded border border-gray-200 p-2 text-sm text-gray-900"
        >
          <div className="aspect-[4/3] w-full overflow-hidden rounded bg-gray-100">
            {p.imageUrl ? (
              <img src={p.imageUrl} alt={p.name} className="h-full w-full object-cover" loading="lazy" />
            ) : null}
          </div>
          <div className="mt-2 font-medium">{p.name}</div>
          <div className="text-gray-700">${p.price.toFixed(2)}</div>
        </Link>
      ))}
    </div>
  );
};
