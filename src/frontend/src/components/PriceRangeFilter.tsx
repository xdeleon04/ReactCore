import React from 'react';

export const PriceRangeFilter: React.FC<{
  minPrice: string;
  maxPrice: string;
  onChange: (next: { minPrice: string; maxPrice: string }) => void;
}> = ({ minPrice, maxPrice, onChange }) => {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-sm font-medium text-gray-900">Price range</legend>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <label htmlFor="minPrice" className="text-sm text-gray-700">
            Min
          </label>
          <input
            id="minPrice"
            type="number"
            inputMode="decimal"
            min={0}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={minPrice}
            onChange={(e) => onChange({ minPrice: e.target.value, maxPrice })}
          />
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="maxPrice" className="text-sm text-gray-700">
            Max
          </label>
          <input
            id="maxPrice"
            type="number"
            inputMode="decimal"
            min={0}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={maxPrice}
            onChange={(e) => onChange({ minPrice, maxPrice: e.target.value })}
          />
        </div>
      </div>
    </fieldset>
  );
};
