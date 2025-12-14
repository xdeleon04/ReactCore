import React from 'react';

export const CartSkeleton: React.FC = () => {
  return (
    <div className="space-y-4" aria-busy="true" aria-label="Loading cart">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="flex gap-3 border-b border-gray-200 py-3">
          <div className="h-16 w-20 flex-shrink-0 animate-pulse rounded bg-gray-100" />
          <div className="min-w-0 flex-1 space-y-2">
            <div className="h-4 w-3/4 animate-pulse rounded bg-gray-100" />
            <div className="h-3 w-1/3 animate-pulse rounded bg-gray-100" />
            <div className="h-8 w-2/3 animate-pulse rounded bg-gray-100" />
          </div>
        </div>
      ))}

      <div className="mt-4 h-4 w-1/2 animate-pulse rounded bg-gray-100" />
    </div>
  );
};
