import React from 'react';

export const ProductCardSkeleton: React.FC = () => {
  return (
    <div className="rounded border border-gray-200 p-3">
      <div className="aspect-[4/3] w-full animate-pulse rounded bg-gray-100" />
      <div className="mt-3 space-y-2">
        <div className="h-4 w-3/4 animate-pulse rounded bg-gray-100" />
        <div className="h-3 w-1/2 animate-pulse rounded bg-gray-100" />
        <div className="h-3 w-2/3 animate-pulse rounded bg-gray-100" />
      </div>
    </div>
  );
};
