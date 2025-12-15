import React from 'react';

export const CategoryFilter: React.FC<{
  categories: string[];
  value: string;
  onChange: (next: string) => void;
}> = ({ categories, value, onChange }) => {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor="category" className="text-sm font-medium text-slate-900">
        Category
      </label>
      <select
        id="category"
        className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 transition duration-300 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/10 focus-visible:border-blue-600"
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
