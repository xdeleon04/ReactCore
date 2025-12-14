import React from 'react';
import type { ProductSpecifications } from '../types/Product';

export const SpecificationsTable: React.FC<{ specifications: ProductSpecifications }> = ({ specifications }) => {
  const entries = Object.entries(specifications ?? {});

  if (entries.length === 0) {
    return <div className="text-sm text-gray-700">No specifications provided.</div>;
  }

  return (
    <table className="w-full border-collapse text-sm">
      <tbody>
        {entries.map(([key, value]) => (
          <tr key={key} className="border-b border-gray-200">
            <th scope="row" className="w-1/3 py-2 pr-4 text-left font-medium text-gray-900">
              {key}
            </th>
            <td className="py-2 text-gray-700">{String(value ?? '')}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
