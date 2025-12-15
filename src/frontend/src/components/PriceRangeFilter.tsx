import React from 'react';
import { Input } from './ui/input';

export const PriceRangeFilter: React.FC<{
  minPrice: string;
  maxPrice: string;
  onChange: (next: { minPrice: string; maxPrice: string }) => void;
}> = ({ minPrice, maxPrice, onChange }) => {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-sm font-medium text-slate-900">Price range</legend>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <label htmlFor="minPrice" className="text-sm text-slate-700">
            Min
          </label>
          <Input
            id="minPrice"
            type="number"
            inputMode="decimal"
            min={0}
            value={minPrice}
            onChange={(e) => onChange({ minPrice: e.target.value, maxPrice })}
          />
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="maxPrice" className="text-sm text-slate-700">
            Max
          </label>
          <Input
            id="maxPrice"
            type="number"
            inputMode="decimal"
            min={0}
            value={maxPrice}
            onChange={(e) => onChange({ minPrice, maxPrice: e.target.value })}
          />
        </div>
      </div>
    </fieldset>
  );
};
