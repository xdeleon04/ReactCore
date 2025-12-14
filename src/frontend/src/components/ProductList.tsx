import React from 'react';
import type { ProductListItem } from '../types/Product';
import { ProductCard } from './ProductCard';
import { ProductCardSkeleton } from './skeletons/ProductCardSkeleton';
import { EmptyState } from './EmptyState';

export const ProductList: React.FC<{
  items: ProductListItem[];
  loading: boolean;
}> = ({ items, loading }) => {
  if (loading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <ProductCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return <EmptyState title="No products found" description="Try adjusting filters and search again." />;
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {items.map((p) => (
        <ProductCard key={p.id} product={p} />
      ))}
    </div>
  );
};
