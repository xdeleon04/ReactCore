import React from 'react';

export const CategoryFilter: React.FC<{
  categories: string[];
  value: string;
  onChange: (next: string) => void;
}> = ({ categories, value, onChange }) => {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor="category" className="text-sm font-medium text-gray-900">
        Category
      </label>
      <select
        id="category"
        className="rounded border border-gray-300 px-3 py-2 text-sm"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">All categories</option>
        {categories.map((c) => (
          <option key={c} value={c}>
            {c}
          </option>
        ))}
      </select>
    </div>
  );
};
