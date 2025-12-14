import React from 'react';

export const ProductDetailSkeleton: React.FC = () => {
  return (
    <main className="mx-auto max-w-6xl px-4 py-6" aria-busy="true" aria-label="Loading product">
      <div className="h-6 w-1/2 animate-pulse rounded bg-gray-100" />
      <div className="mt-4 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="aspect-[4/3] w-full animate-pulse rounded bg-gray-100" />
        <div className="space-y-3">
          <div className="h-4 w-3/4 animate-pulse rounded bg-gray-100" />
          <div className="h-4 w-1/3 animate-pulse rounded bg-gray-100" />
          <div className="h-10 w-40 animate-pulse rounded bg-gray-100" />
        </div>
      </div>
    </main>
  );
};
